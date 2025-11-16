using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using WaterPolo.Players;
using WaterPolo.Tactics;

namespace WaterPolo.Core
{
    /// <summary>
    /// Manages a water polo team: active players, bench, exclusions.
    /// Handles substitutions and team state.
    /// </summary>
    public class TeamManager : MonoBehaviour
    {
        [Header("Team Identity")]
        [SerializeField] private string _teamName = "Home";
        [SerializeField] private Color _teamColor = Color.blue;

        [Header("Roster")]
        [SerializeField] private List<WaterPoloPlayer> _fullRoster = new List<WaterPoloPlayer>();
        [SerializeField] private List<WaterPoloPlayer> _activePlayers = new List<WaterPoloPlayer>();
        [SerializeField] private List<WaterPoloPlayer> _benchPlayers = new List<WaterPoloPlayer>();
        [SerializeField] private List<WaterPoloPlayer> _excludedPlayers = new List<WaterPoloPlayer>();

        [Header("Formation")]
        [SerializeField] private FormationManager _formationManager;

        [Header("Goals")]
        [SerializeField] private Transform _ownGoal;
        [SerializeField] private Transform _opponentGoal;

        private const int MAX_ACTIVE_PLAYERS = 7; // 6 field + 1 goalkeeper

        #region Properties

        public string TeamName => _teamName;
        public Color TeamColor => _teamColor;
        public int ActivePlayerCount => _activePlayers.Count;
        public int BenchPlayerCount => _benchPlayers.Count;
        public FormationManager FormationManager => _formationManager;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Find formation manager if not assigned
            if (_formationManager == null)
            {
                _formationManager = GetComponent<FormationManager>();
            }

            // Subscribe to events
            EventBus.Instance.Subscribe<ExclusionStartedEvent>(OnExclusionStarted);
            EventBus.Instance.Subscribe<ExclusionEndedEvent>(OnExclusionEnded);
        }

        private void OnDestroy()
        {
            EventBus.Instance.Unsubscribe<ExclusionStartedEvent>(OnExclusionStarted);
            EventBus.Instance.Unsubscribe<ExclusionEndedEvent>(OnExclusionEnded);
        }

        private void Start()
        {
            // Initialize roster
            InitializeRoster();
        }

        #endregion

        #region Initialization

        private void InitializeRoster()
        {
            // Find all players on this team
            if (_fullRoster.Count == 0)
            {
                WaterPoloPlayer[] allPlayers = FindObjectsOfType<WaterPoloPlayer>();
                foreach (var player in allPlayers)
                {
                    if (player.TeamName == _teamName)
                    {
                        _fullRoster.Add(player);
                    }
                }
            }

            // Setup initial active players (first 7)
            _activePlayers.Clear();
            _benchPlayers.Clear();

            // Ensure we have a goalkeeper
            WaterPoloPlayer goalkeeper = _fullRoster.FirstOrDefault(p => p.Role == PlayerRole.Goalkeeper);
            if (goalkeeper != null)
            {
                _activePlayers.Add(goalkeeper);
            }

            // Add field players up to 7 total
            foreach (var player in _fullRoster)
            {
                if (_activePlayers.Count >= MAX_ACTIVE_PLAYERS)
                    break;

                if (player.Role != PlayerRole.Goalkeeper && !_activePlayers.Contains(player))
                {
                    _activePlayers.Add(player);
                }
            }

            // Rest go to bench
            foreach (var player in _fullRoster)
            {
                if (!_activePlayers.Contains(player))
                {
                    _benchPlayers.Add(player);
                    player.gameObject.SetActive(false); // Bench players inactive
                }
            }

            // Register active players with formation manager
            if (_formationManager != null)
            {
                foreach (var player in _activePlayers)
                {
                    _formationManager.RegisterPlayer(player);
                }
            }

            Debug.Log($"Team {_teamName} initialized: {_activePlayers.Count} active, {_benchPlayers.Count} bench");
        }

        #endregion

        #region Player Management

        /// <summary>
        /// Add a player to the roster.
        /// </summary>
        public void AddPlayer(WaterPoloPlayer player)
        {
            if (_fullRoster.Contains(player))
            {
                Debug.LogWarning($"Player {player.PlayerName} already in roster!");
                return;
            }

            _fullRoster.Add(player);

            // If we have room, make them active
            if (_activePlayers.Count < MAX_ACTIVE_PLAYERS)
            {
                ActivatePlayer(player);
            }
            else
            {
                // Send to bench
                _benchPlayers.Add(player);
                player.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Remove a player from the roster.
        /// </summary>
        public void RemovePlayer(WaterPoloPlayer player)
        {
            _fullRoster.Remove(player);
            _activePlayers.Remove(player);
            _benchPlayers.Remove(player);
            _excludedPlayers.Remove(player);

            if (_formationManager != null)
            {
                _formationManager.UnregisterPlayer(player);
            }
        }

        private void ActivatePlayer(WaterPoloPlayer player)
        {
            if (_activePlayers.Contains(player))
                return;

            _activePlayers.Add(player);
            _benchPlayers.Remove(player);

            player.gameObject.SetActive(true);

            if (_formationManager != null)
            {
                _formationManager.RegisterPlayer(player);
            }
        }

        private void DeactivatePlayer(WaterPoloPlayer player)
        {
            _activePlayers.Remove(player);

            if (!_benchPlayers.Contains(player) && !_excludedPlayers.Contains(player))
            {
                _benchPlayers.Add(player);
            }

            player.gameObject.SetActive(false);

            if (_formationManager != null)
            {
                _formationManager.UnregisterPlayer(player);
            }
        }

        #endregion

        #region Substitutions

        /// <summary>
        /// Substitute a player (remove from field, bring from bench).
        /// </summary>
        public bool SubstitutePlayer(WaterPoloPlayer playerOut, WaterPoloPlayer playerIn)
        {
            // Validate substitution
            if (!_activePlayers.Contains(playerOut))
            {
                Debug.LogWarning($"Cannot substitute - {playerOut.PlayerName} not active!");
                return false;
            }

            if (!_benchPlayers.Contains(playerIn))
            {
                Debug.LogWarning($"Cannot substitute - {playerIn.PlayerName} not on bench!");
                return false;
            }

            // Perform substitution
            DeactivatePlayer(playerOut);
            ActivatePlayer(playerIn);

            Debug.Log($"Substitution: {playerOut.PlayerName} OUT, {playerIn.PlayerName} IN");

            return true;
        }

        /// <summary>
        /// Find best replacement for a role (when player excluded/injured).
        /// </summary>
        public WaterPoloPlayer FindReplacementForRole(PlayerRole role)
        {
            // Try to find bench player with same role
            WaterPoloPlayer replacement = _benchPlayers.FirstOrDefault(p => p.Role == role);

            if (replacement == null)
            {
                // Any bench player will do
                replacement = _benchPlayers.FirstOrDefault();
            }

            return replacement;
        }

        #endregion

        #region Exclusion Management

        private void OnExclusionStarted(ExclusionStartedEvent evt)
        {
            WaterPoloPlayer player = evt.Player as WaterPoloPlayer;

            if (player == null || player.TeamName != _teamName)
                return;

            // Move to excluded list temporarily
            if (_activePlayers.Contains(player))
            {
                _excludedPlayers.Add(player);
                // Don't remove from active - they're still "in game", just excluded
                // Just move them to penalty area (Phase 4)
            }

            Debug.Log($"Team {_teamName}: Player {player.PlayerName} excluded for {evt.Duration}s");
        }

        private void OnExclusionEnded(ExclusionEndedEvent evt)
        {
            WaterPoloPlayer player = evt.Player as WaterPoloPlayer;

            if (player == null || player.TeamName != _teamName)
                return;

            _excludedPlayers.Remove(player);

            Debug.Log($"Team {_teamName}: Player {player.PlayerName} exclusion ended, returning to play");
        }

        #endregion

        #region Queries

        /// <summary>
        /// Get all active players.
        /// </summary>
        public List<WaterPoloPlayer> GetActivePlayers()
        {
            return new List<WaterPoloPlayer>(_activePlayers);
        }

        /// <summary>
        /// Get all bench players.
        /// </summary>
        public List<WaterPoloPlayer> GetBenchPlayers()
        {
            return new List<WaterPoloPlayer>(_benchPlayers);
        }

        /// <summary>
        /// Get currently excluded players.
        /// </summary>
        public List<WaterPoloPlayer> GetExcludedPlayers()
        {
            return new List<WaterPoloPlayer>(_excludedPlayers);
        }

        /// <summary>
        /// Get player with specific role.
        /// </summary>
        public WaterPoloPlayer GetPlayerByRole(PlayerRole role)
        {
            return _activePlayers.FirstOrDefault(p => p.Role == role);
        }

        /// <summary>
        /// Check if team is at numerical disadvantage.
        /// </summary>
        public bool IsNumericallyDisadvantaged()
        {
            int playingCount = _activePlayers.Count - _excludedPlayers.Count;
            return playingCount < 7;
        }

        /// <summary>
        /// Get number of players currently on field (not excluded).
        /// </summary>
        public int GetFieldPlayerCount()
        {
            return _activePlayers.Count - _excludedPlayers.Count;
        }

        #endregion

        #region Goal References

        public void SetOwnGoal(Transform goal)
        {
            _ownGoal = goal;
        }

        public void SetOpponentGoal(Transform goal)
        {
            _opponentGoal = goal;
        }

        public Transform GetOwnGoal() => _ownGoal;
        public Transform GetOpponentGoal() => _opponentGoal;

        #endregion
    }
}
