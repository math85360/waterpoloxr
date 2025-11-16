using UnityEngine;
using WaterPolo.Core;
using WaterPolo.Players;

namespace WaterPolo.Tactics
{
    /// <summary>
    /// Types of defensive tactics.
    /// </summary>
    public enum DefenseType
    {
        ManToMan,    // Each defender marks specific attacker
        Zone,        // Defenders cover areas, not players
        Pressing,    // Aggressive pressure on ball carrier
        Wall         // Emergency defense (2+ players down)
    }

    /// <summary>
    /// Types of offensive tactics.
    /// </summary>
    public enum OffenseType
    {
        Standard,        // Normal balanced attack
        ThroughPivot,    // Play through center forward
        FastBreak,       // Quick counter-attack
        PerimeterShot,   // Long-range shooting
        DrawAndKick      // Draw defender, pass to open player
    }

    /// <summary>
    /// Manages tactical decisions for a team.
    /// Adapts tactics based on game situation and opponent.
    /// </summary>
    public class TeamTactics : MonoBehaviour
    {
        [Header("Current Tactics")]
        [SerializeField] private DefenseType _currentDefense = DefenseType.ManToMan;
        [SerializeField] private OffenseType _currentOffense = OffenseType.Standard;

        [Header("Tactical Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float _aggressiveness = 0.5f; // How aggressive overall

        [Range(0f, 1f)]
        [SerializeField] private float _riskTolerance = 0.5f; // Willingness to take risks

        [Range(0f, 1f)]
        [SerializeField] private float _possessionOriented = 0.5f; // Hold ball vs shoot quickly

        [Header("Adaptations")]
        [SerializeField] private bool _autoAdaptToNumericalDisadvantage = true;
        [SerializeField] private bool _autoAdaptToOpponentDefense = true;

        [Header("References")]
        [SerializeField] private TeamManager _teamManager;
        [SerializeField] private FormationManager _formationManager;

        private DefenseType _detectedOpponentDefense = DefenseType.ManToMan;
        private float _opponentDefenseConfidence = 0f;

        #region Properties

        public DefenseType CurrentDefense => _currentDefense;
        public OffenseType CurrentOffense => _currentOffense;
        public float Aggressiveness => _aggressiveness;
        public float RiskTolerance => _riskTolerance;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_teamManager == null)
                _teamManager = GetComponent<TeamManager>();

            if (_formationManager == null)
                _formationManager = GetComponent<FormationManager>();

            // Subscribe to events
            EventBus.Instance.Subscribe<ExclusionStartedEvent>(OnExclusionStarted);
            EventBus.Instance.Subscribe<ExclusionEndedEvent>(OnExclusionEnded);
        }

        private void OnDestroy()
        {
            EventBus.Instance.Unsubscribe<ExclusionStartedEvent>(OnExclusionStarted);
            EventBus.Instance.Unsubscribe<ExclusionEndedEvent>(OnExclusionEnded);
        }

        private void Update()
        {
            // Periodic tactical evaluation
            if (Time.frameCount % 60 == 0) // Once per second roughly
            {
                EvaluateTactics();
            }
        }

        #endregion

        #region Tactical Evaluation

        private void EvaluateTactics()
        {
            // Check for numerical disadvantage
            if (_autoAdaptToNumericalDisadvantage && _teamManager != null)
            {
                int fieldPlayers = _teamManager.GetFieldPlayerCount();

                if (fieldPlayers <= 5) // 2+ players down
                {
                    AdaptToNumericalDisadvantage(fieldPlayers);
                }
                else if (fieldPlayers == 6) // 1 player down
                {
                    // Switch to zone defense
                    if (_currentDefense != DefenseType.Zone)
                    {
                        SetDefense(DefenseType.Zone, "Numerical disadvantage (1 player)");
                    }
                }
            }

            // Other evaluations...
        }

        #endregion

        #region Tactical Changes

        /// <summary>
        /// Set defensive tactic.
        /// </summary>
        public void SetDefense(DefenseType type, string reason = "")
        {
            if (_currentDefense == type) return;

            DefenseType previous = _currentDefense;
            _currentDefense = type;

            OnDefenseChanged(previous, type, reason);

            // Publish event
            EventBus.Instance.Publish(new TacticalAdaptationEvent(
                TacticType.Defense,
                previous.ToString(),
                type.ToString(),
                reason
            ));

            Debug.Log($"Defense changed: {previous} → {type} ({reason})");
        }

        /// <summary>
        /// Set offensive tactic.
        /// </summary>
        public void SetOffense(OffenseType type, string reason = "")
        {
            if (_currentOffense == type) return;

            OffenseType previous = _currentOffense;
            _currentOffense = type;

            OnOffenseChanged(previous, type, reason);

            Debug.Log($"Offense changed: {previous} → {type} ({reason})");
        }

        private void OnDefenseChanged(DefenseType previous, DefenseType current, string reason)
        {
            // Apply defensive adjustments to formation/players
            switch (current)
            {
                case DefenseType.Zone:
                    _aggressiveness = Mathf.Max(0.3f, _aggressiveness - 0.2f);
                    break;

                case DefenseType.Pressing:
                    _aggressiveness = Mathf.Min(1f, _aggressiveness + 0.3f);
                    break;

                case DefenseType.Wall:
                    _aggressiveness = 0.1f; // Very passive
                    break;

                case DefenseType.ManToMan:
                    // Reset to default
                    break;
            }

            // Update formation manager if needed
            // Phase 4 will have formation changes based on defense type
        }

        private void OnOffenseChanged(OffenseType previous, OffenseType current, string reason)
        {
            // Apply offensive adjustments
            switch (current)
            {
                case OffenseType.FastBreak:
                    _riskTolerance = Mathf.Min(1f, _riskTolerance + 0.3f);
                    break;

                case OffenseType.ThroughPivot:
                    _possessionOriented = Mathf.Min(1f, _possessionOriented + 0.2f);
                    break;

                case OffenseType.PerimeterShot:
                    _riskTolerance = Mathf.Max(0.3f, _riskTolerance - 0.2f);
                    break;
            }
        }

        #endregion

        #region Adaptations

        /// <summary>
        /// Adapt tactics when team has numerical disadvantage.
        /// </summary>
        private void AdaptToNumericalDisadvantage(int fieldPlayers)
        {
            if (fieldPlayers <= 5)
            {
                // 2+ players down = WALL defense
                SetDefense(DefenseType.Wall, $"Severe numerical disadvantage ({7 - fieldPlayers} players down)");
            }
            else if (fieldPlayers == 6)
            {
                // 1 player down = Zone defense
                SetDefense(DefenseType.Zone, "Numerical disadvantage (1 player down)");
            }
        }

        /// <summary>
        /// Adapt offense based on detected opponent defense type.
        /// </summary>
        public void AdaptToOpponentDefense(DefenseType opponentDefense, float confidence)
        {
            if (!_autoAdaptToOpponentDefense) return;
            if (confidence < 0.7f) return; // Not confident enough

            _detectedOpponentDefense = opponentDefense;
            _opponentDefenseConfidence = confidence;

            // Counter-tactics
            switch (opponentDefense)
            {
                case DefenseType.Zone:
                    // Against zone: shoot from distance, find intervals
                    SetOffense(OffenseType.PerimeterShot, "Counter opponent zone defense");
                    break;

                case DefenseType.Pressing:
                    // Against pressing: draw fouls, quick passes
                    SetOffense(OffenseType.DrawAndKick, "Counter opponent pressing");
                    _riskTolerance = 0.3f; // Be careful
                    break;

                case DefenseType.ManToMan:
                    // Against man-to-man: use screens, isolations
                    SetOffense(OffenseType.ThroughPivot, "Counter opponent man-to-man");
                    break;
            }

            Debug.Log($"Adapted to opponent defense: {opponentDefense} (confidence: {confidence:F2})");
        }

        #endregion

        #region Event Handlers

        private void OnExclusionStarted(ExclusionStartedEvent evt)
        {
            WaterPoloPlayer player = evt.Player as WaterPoloPlayer;

            if (player == null) return;

            // Check if it's our team's player
            if (_teamManager != null && _teamManager.GetActivePlayers().Contains(player))
            {
                // Our player excluded - adapt defense
                if (_autoAdaptToNumericalDisadvantage)
                {
                    int fieldPlayers = _teamManager.GetFieldPlayerCount();
                    AdaptToNumericalDisadvantage(fieldPlayers);
                }
            }
        }

        private void OnExclusionEnded(ExclusionEndedEvent evt)
        {
            WaterPoloPlayer player = evt.Player as WaterPoloPlayer;

            if (player == null) return;

            // Check if back to full strength
            if (_teamManager != null)
            {
                int fieldPlayers = _teamManager.GetFieldPlayerCount();

                if (fieldPlayers >= 7)
                {
                    // Back to full strength - revert to standard defense
                    SetDefense(DefenseType.ManToMan, "Back to full strength");
                }
            }
        }

        #endregion

        #region Decision Support

        /// <summary>
        /// Should player attempt a shot in current tactical context?
        /// </summary>
        public bool ShouldAttemptShot(WaterPoloPlayer player, float distanceToGoal)
        {
            // Base decision on offense type and distance
            switch (_currentOffense)
            {
                case OffenseType.PerimeterShot:
                    return distanceToGoal < 12f; // Long-range shots OK

                case OffenseType.FastBreak:
                    return true; // Shoot quickly

                case OffenseType.ThroughPivot:
                    // Only shoot if close or you're the pivot
                    return distanceToGoal < 6f || player.Role == PlayerRole.CenterForward;

                default:
                    return distanceToGoal < 8f; // Standard range
            }
        }

        /// <summary>
        /// Get recommended passing aggressiveness.
        /// </summary>
        public float GetPassingAggressiveness()
        {
            return Mathf.Lerp(0.3f, 1f, 1f - _possessionOriented);
        }

        #endregion
    }

    #region Event Classes

    public enum TacticType
    {
        Defense,
        Offense,
        Formation
    }

    public class TacticalAdaptationEvent : GameEvent
    {
        public TacticType Type { get; private set; }
        public string OldTactic { get; private set; }
        public string NewTactic { get; private set; }
        public string Reason { get; private set; }

        public TacticalAdaptationEvent(TacticType type, string oldTactic, string newTactic, string reason)
        {
            Type = type;
            OldTactic = oldTactic;
            NewTactic = newTactic;
            Reason = reason;
        }
    }

    #endregion
}
