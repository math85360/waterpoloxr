using UnityEngine;
using System.Collections.Generic;
using WaterPolo.Core;
using WaterPolo.Players;

namespace WaterPolo.Referee
{
    /// <summary>
    /// Types of fouls in water polo.
    /// </summary>
    public enum FoulType
    {
        None,
        Holding,              // Retenir bras/maillot
        Sinking,              // Couler joueur
        Obstruction,          // Obstruction
        Brutality,            // Brutalité (violence)
        BallUnderwater,       // Ballon sous l'eau
        ShotClockViolation,   // Shot clock expiré
        IllegalMovement,      // Mouvement illégal
        GoalkeeperViolation,  // Faute gardien
        TwoMeterViolation,    // Joueur dans zone 2m sans ballon
        PushingOff,           // Prendre appui sur adversaire
        Interference          // Interférence sur tireur
    }

    /// <summary>
    /// Sanction levels for fouls.
    /// </summary>
    public enum FoulSanction
    {
        None,
        OrdinaryFoul,         // Faute ordinaire (coup franc)
        Exclusion,            // Exclusion 20s
        Penalty,              // Pénalty (5m)
        ExclusionAndPenalty,  // Exclusion + pénalty
        PermanentExclusion    // Exclusion définitive (carton rouge)
    }

    /// <summary>
    /// Referee state machine states.
    /// </summary>
    public enum RefereeState
    {
        OBSERVING,           // Watching play
        ADVANTAGE_PENDING,   // Foul detected, waiting for advantage
        WHISTLING,           // Blowing whistle
        FOUL_MANAGEMENT,     // Managing foul (positioning players)
        RESUMING             // Resuming play
    }

    /// <summary>
    /// Represents a detected foul event.
    /// </summary>
    [System.Serializable]
    public class FoulEvent
    {
        public FoulType foulType;
        public WaterPoloPlayer offender;
        public WaterPoloPlayer victim;
        public Vector3 location;
        public float severity; // 0-1
        public float timestamp;
        public bool isBrutality;
        public bool preventedGoalOpportunity;

        public FoulEvent(FoulType type, WaterPoloPlayer offender, WaterPoloPlayer victim, Vector3 location, float severity)
        {
            this.foulType = type;
            this.offender = offender;
            this.victim = victim;
            this.location = location;
            this.severity = severity;
            this.timestamp = Time.time;
        }
    }

    /// <summary>
    /// Autonomous referee system that detects fouls and applies rules.
    /// Phase 2: Basic foul detection.
    /// Phase 4: Advanced detection with advantage rule, errors, context.
    /// </summary>
    public class RefereeSystem : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private RefereeProfile _profile;

        [Header("State")]
        [SerializeField] private RefereeState _currentState = RefereeState.OBSERVING;
        [SerializeField] private FoulEvent _pendingFoul;
        [SerializeField] private float _advantageTimer;

        [Header("Detection Zones")]
        [SerializeField] private Transform _twoMeterLineHome;
        [SerializeField] private Transform _twoMeterLineAway;

        [Header("References")]
        [SerializeField] private GameClock _gameClock;
        [SerializeField] private MatchState _matchState;
        [SerializeField] private ScoreTable _scoreTable;

        private Dictionary<WaterPoloPlayer, PlayerFoulRecord> _foulRecords = new Dictionary<WaterPoloPlayer, PlayerFoulRecord>();

        #region Unity Lifecycle

        private void Awake()
        {
            // Find references if not assigned
            if (_gameClock == null)
                _gameClock = FindObjectOfType<GameClock>();

            if (_matchState == null)
                _matchState = FindObjectOfType<MatchState>();

            if (_scoreTable == null)
                _scoreTable = FindObjectOfType<ScoreTable>();

            // Load default profile if none assigned
            if (_profile == null)
            {
                Debug.LogWarning("RefereeSystem: No profile assigned! Creating default.");
                _profile = ScriptableObject.CreateInstance<RefereeProfile>();
            }
        }

        private void Start()
        {
            // Subscribe to events
            EventBus.Instance.Subscribe<ShotClockExpiredEvent>(OnShotClockExpired);
        }

        private void OnDestroy()
        {
            EventBus.Instance.Unsubscribe<ShotClockExpiredEvent>(OnShotClockExpired);
        }

        private void Update()
        {
            if (_matchState == null || !_matchState.IsPlaying)
                return;

            // Update state machine
            switch (_currentState)
            {
                case RefereeState.OBSERVING:
                    ObservePlay();
                    break;

                case RefereeState.ADVANTAGE_PENDING:
                    UpdateAdvantage();
                    break;

                case RefereeState.FOUL_MANAGEMENT:
                    ManageFoul();
                    break;
            }
        }

        #endregion

        #region State Machine

        private void ObservePlay()
        {
            // Phase 2: Basic observation
            // Detect obvious fouls like 2-meter violations

            DetectTwoMeterViolations();

            // Phase 4 will add more sophisticated detection
        }

        private void UpdateAdvantage()
        {
            _advantageTimer += Time.deltaTime;

            // Check if advantage time expired
            if (_advantageTimer >= _profile.maxAdvantageTime)
            {
                // Call the pending foul
                CallFoul(_pendingFoul);
                return;
            }

            // Check if offensive action succeeded (goal scored)
            // If yes, ignore foul (unless brutality)
            // This is handled by event subscription

            // Check if offensive action failed
            // For Phase 2, simplified: if ball possession lost, call foul
        }

        private void ManageFoul()
        {
            // Phase 2: Simplified - just transition back to playing after delay
            // Phase 4 will add positioning management, hand checks, etc.

            // For now, wait a moment then resume
            if (Time.time - _pendingFoul.timestamp > 2f)
            {
                ResumePlay();
            }
        }

        #endregion

        #region Foul Detection

        /// <summary>
        /// Detect players in 2-meter zone without ball.
        /// </summary>
        private void DetectTwoMeterViolations()
        {
            // Phase 2: Simplified detection
            // Check if attackers are in 2m zone without ball nearby

            // This would require collision detection or zone triggers
            // For now, placeholder for Phase 2
        }

        /// <summary>
        /// Manually report a foul (called by other systems or collision detection).
        /// </summary>
        public void ReportFoul(FoulEvent foul)
        {
            // Determine if foul should be called
            bool hasAdvantageOpportunity = CheckAdvantageOpportunity(foul);

            if (_profile.ShouldCallFoul(foul.severity, hasAdvantageOpportunity))
            {
                if (hasAdvantageOpportunity && !foul.isBrutality)
                {
                    // Wait for advantage
                    _pendingFoul = foul;
                    _advantageTimer = 0f;
                    _currentState = RefereeState.ADVANTAGE_PENDING;

                    Debug.Log($"Referee: Advantage pending for {foul.foulType}");
                }
                else
                {
                    // Call immediately
                    CallFoul(foul);
                }
            }
            else
            {
                Debug.Log($"Referee: Ignoring minor foul {foul.foulType}");
            }
        }

        private bool CheckAdvantageOpportunity(FoulEvent foul)
        {
            // Check if victim's team has offensive opportunity
            if (foul.victim == null) return false;

            // Simplified: Check if near opponent goal
            // Phase 4 will have more sophisticated logic

            return false; // Placeholder for Phase 2
        }

        #endregion

        #region Foul Calling

        private void CallFoul(FoulEvent foul)
        {
            _currentState = RefereeState.WHISTLING;

            // Determine sanction
            FoulSanction sanction = DetermineSanction(foul);

            // Stop clocks
            _gameClock?.StopClock();

            // Update match state
            if (_matchState != null)
            {
                switch (sanction)
                {
                    case FoulSanction.OrdinaryFoul:
                        _matchState.TransitionToState(MatchStateType.FREE_THROW);
                        break;

                    case FoulSanction.Penalty:
                    case FoulSanction.ExclusionAndPenalty:
                        _matchState.TransitionToState(MatchStateType.PENALTY);
                        break;

                    case FoulSanction.Exclusion:
                        _matchState.TransitionToState(MatchStateType.EXCLUSION);
                        break;
                }
            }

            // Apply sanction
            ApplySanction(foul, sanction);

            // Record foul
            RecordFoul(foul, sanction);

            // Update statistics
            if (_scoreTable != null && foul.offender != null)
            {
                _scoreTable.RecordFoul(foul.offender.TeamName);
            }

            Debug.Log($"WHISTLE! {foul.foulType} called on {foul.offender?.PlayerName ?? "Unknown"} - Sanction: {sanction}");

            // Transition to foul management
            _currentState = RefereeState.FOUL_MANAGEMENT;
        }

        private FoulSanction DetermineSanction(FoulEvent foul)
        {
            // Check for penalty
            if (_profile.ShouldCallPenalty(foul.severity, foul.preventedGoalOpportunity))
            {
                // Check if also exclusion
                if (_profile.ShouldCallExclusion(foul.severity, foul.isBrutality))
                {
                    return FoulSanction.ExclusionAndPenalty;
                }
                return FoulSanction.Penalty;
            }

            // Check for exclusion
            if (_profile.ShouldCallExclusion(foul.severity, foul.isBrutality))
            {
                // Check for permanent exclusion
                if (ShouldPermanentlyExclude(foul.offender))
                {
                    return FoulSanction.PermanentExclusion;
                }
                return FoulSanction.Exclusion;
            }

            // Ordinary foul
            return FoulSanction.OrdinaryFoul;
        }

        private void ApplySanction(FoulEvent foul, FoulSanction sanction)
        {
            if (foul.offender == null) return;

            switch (sanction)
            {
                case FoulSanction.OrdinaryFoul:
                    // Just a free throw, no further action
                    break;

                case FoulSanction.Exclusion:
                    StartExclusion(foul.offender, 20f);
                    if (_scoreTable != null)
                    {
                        _scoreTable.RecordExclusion(foul.offender.TeamName);
                    }
                    break;

                case FoulSanction.Penalty:
                    // Penalty shot will be set up by MatchState
                    break;

                case FoulSanction.ExclusionAndPenalty:
                    StartExclusion(foul.offender, 20f);
                    if (_scoreTable != null)
                    {
                        _scoreTable.RecordExclusion(foul.offender.TeamName);
                    }
                    break;

                case FoulSanction.PermanentExclusion:
                    PermanentlyExcludePlayer(foul.offender);
                    break;
            }

            // Publish event
            EventBus.Instance.Publish(new FoulDetectedEvent(foul, sanction));
        }

        #endregion

        #region Exclusions

        private void StartExclusion(WaterPoloPlayer player, float duration)
        {
            if (_gameClock == null) return;

            _gameClock.StartExclusion(player, duration);

            Debug.Log($"Player {player.PlayerName} excluded for {duration}s");
        }

        private void PermanentlyExcludePlayer(WaterPoloPlayer player)
        {
            // Player cannot return
            Debug.Log($"Player {player.PlayerName} PERMANENTLY EXCLUDED (red card)");

            // Deactivate player
            player.gameObject.SetActive(false);

            // TODO Phase 4: Notify team manager for replacement
        }

        private bool ShouldPermanentlyExclude(WaterPoloPlayer player)
        {
            // Check foul record
            if (!_foulRecords.ContainsKey(player))
                return false;

            PlayerFoulRecord record = _foulRecords[player];

            // 3 major sanctions = permanent exclusion
            return (record.exclusions + record.penalties) >= 3;
        }

        #endregion

        #region Foul Records

        private void RecordFoul(FoulEvent foul, FoulSanction sanction)
        {
            if (foul.offender == null) return;

            if (!_foulRecords.ContainsKey(foul.offender))
            {
                _foulRecords[foul.offender] = new PlayerFoulRecord();
            }

            PlayerFoulRecord record = _foulRecords[foul.offender];
            record.ordinaryFouls++;

            if (sanction == FoulSanction.Exclusion || sanction == FoulSanction.ExclusionAndPenalty)
            {
                record.exclusions++;
            }

            if (sanction == FoulSanction.Penalty || sanction == FoulSanction.ExclusionAndPenalty)
            {
                record.penalties++;
            }

            if (sanction == FoulSanction.PermanentExclusion)
            {
                record.permanentExclusion = true;
            }
        }

        public PlayerFoulRecord GetPlayerRecord(WaterPoloPlayer player)
        {
            if (_foulRecords.ContainsKey(player))
                return _foulRecords[player];

            return new PlayerFoulRecord();
        }

        #endregion

        #region Resume Play

        private void ResumePlay()
        {
            _currentState = RefereeState.OBSERVING;
            _pendingFoul = null;

            // Resume clocks
            _gameClock?.StartClock();

            // Transition match state back to playing
            if (_matchState != null && _matchState.CurrentState != MatchStateType.PLAYING)
            {
                _matchState.TransitionToState(MatchStateType.PLAYING);
            }

            Debug.Log("Referee: Play resumed");
        }

        #endregion

        #region Event Handlers

        private void OnShotClockExpired(ShotClockExpiredEvent evt)
        {
            // Create violation foul
            FoulEvent violation = new FoulEvent(
                FoulType.ShotClockViolation,
                null, // Team foul
                null,
                Vector3.zero,
                1.0f
            );

            CallFoul(violation);
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Tracks fouls for a single player during a match.
    /// </summary>
    [System.Serializable]
    public class PlayerFoulRecord
    {
        public int ordinaryFouls = 0;
        public int exclusions = 0;
        public int penalties = 0;
        public bool permanentExclusion = false;

        public int TotalMajorSanctions => exclusions + penalties;
    }

    // Event class for foul detection
    public class FoulDetectedEvent : GameEvent
    {
        public FoulEvent Foul { get; private set; }
        public FoulSanction Sanction { get; private set; }

        public FoulDetectedEvent(FoulEvent foul, FoulSanction sanction)
        {
            Foul = foul;
            Sanction = sanction;
        }
    }

    #endregion
}
