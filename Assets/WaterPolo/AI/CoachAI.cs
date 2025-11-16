using UnityEngine;
using WaterPolo.Core;
using WaterPolo.Tactics;
using WaterPolo.Players;

namespace WaterPolo.AI
{
    /// <summary>
    /// AI Coach that makes high-level tactical decisions for a team.
    /// Decides formations, substitutions, and tactical adaptations.
    /// </summary>
    public class CoachAI : MonoBehaviour
    {
        [Header("Team")]
        [SerializeField] private TeamManager _teamManager;
        [SerializeField] private TeamTactics _teamTactics;
        [SerializeField] private FormationManager _formationManager;

        [Header("Formations")]
        [SerializeField] private WaterPoloFormation _defaultFormation;
        [SerializeField] private WaterPoloFormation _defensiveFormation;
        [SerializeField] private WaterPoloFormation _offensiveFormation;

        [Header("Coach Personality")]
        [Range(0f, 1f)]
        [SerializeField] private float _tacticalFlexibility = 0.7f; // Willingness to change tactics

        [Range(0f, 1f)]
        [SerializeField] private float _riskTolerance = 0.5f;

        [Range(0f, 1f)]
        [SerializeField] private float _substitutionFrequency = 0.3f; // How often to rotate players

        [Header("Game Context")]
        [SerializeField] private GameClock _gameClock;
        [SerializeField] private ScoreTable _scoreTable;

        private float _nextTacticalReview = 0f;
        private const float TACTICAL_REVIEW_INTERVAL = 10f; // Seconds

        #region Unity Lifecycle

        private void Awake()
        {
            // Find components if not assigned
            if (_teamManager == null)
                _teamManager = GetComponent<TeamManager>();

            if (_teamTactics == null)
                _teamTactics = GetComponent<TeamTactics>();

            if (_formationManager == null)
                _formationManager = GetComponent<FormationManager>();

            if (_gameClock == null)
                _gameClock = FindObjectOfType<GameClock>();

            if (_scoreTable == null)
                _scoreTable = FindObjectOfType<ScoreTable>();

            // Subscribe to events
            EventBus.Instance.Subscribe<QuarterEndedEvent>(OnQuarterEnded);
            EventBus.Instance.Subscribe<GoalScoredEvent>(OnGoalScored);
        }

        private void OnDestroy()
        {
            EventBus.Instance.Unsubscribe<QuarterEndedEvent>(OnQuarterEnded);
            EventBus.Instance.Unsubscribe<GoalScoredEvent>(OnGoalScored);
        }

        private void Start()
        {
            // Set initial formation
            if (_defaultFormation != null && _formationManager != null)
            {
                _formationManager.SetFormation(_defaultFormation);
            }
        }

        private void Update()
        {
            // Periodic tactical reviews
            if (Time.time >= _nextTacticalReview)
            {
                ReviewTactics();
                _nextTacticalReview = Time.time + TACTICAL_REVIEW_INTERVAL;
            }
        }

        #endregion

        #region Tactical Reviews

        private void ReviewTactics()
        {
            if (_teamManager == null || _teamTactics == null)
                return;

            // Analyze current game state
            GameContext context = AnalyzeGameContext();

            // Make tactical decisions
            DecideFormation(context);
            DecideTactics(context);
            ConsiderSubstitutions(context);
        }

        private GameContext AnalyzeGameContext()
        {
            GameContext context = new GameContext();

            // Score situation
            if (_scoreTable != null && _teamManager != null)
            {
                int ourScore = _scoreTable.HomeTeamName == _teamManager.TeamName ?
                    _scoreTable.HomeScore : _scoreTable.AwayScore;

                int theirScore = _scoreTable.HomeTeamName == _teamManager.TeamName ?
                    _scoreTable.AwayScore : _scoreTable.HomeScore;

                context.scoreDifference = ourScore - theirScore;
                context.isWinning = context.scoreDifference > 0;
                context.isLosing = context.scoreDifference < 0;
            }

            // Time remaining
            if (_gameClock != null)
            {
                context.quarterTimeRemaining = _gameClock.QuarterTimeRemaining;
                context.currentQuarter = _gameClock.CurrentQuarter;
                context.isEndGame = context.currentQuarter == 4 && context.quarterTimeRemaining < 120f; // Last 2 minutes
            }

            // Numerical situation
            if (_teamManager != null)
            {
                context.fieldPlayerCount = _teamManager.GetFieldPlayerCount();
                context.isNumericallyDisadvantaged = context.fieldPlayerCount < 7;
            }

            return context;
        }

        #endregion

        #region Formation Decisions

        private void DecideFormation(GameContext context)
        {
            if (_formationManager == null) return;

            WaterPoloFormation targetFormation = _defaultFormation;

            // Decide based on context
            if (context.isNumericallyDisadvantaged)
            {
                // Stay with current formation when down players
                // Formation changes handled by TeamTactics automatically
                return;
            }

            if (context.isEndGame)
            {
                if (context.isLosing)
                {
                    // Need to score - offensive formation
                    targetFormation = _offensiveFormation ?? _defaultFormation;
                }
                else if (context.isWinning && Mathf.Abs(context.scoreDifference) <= 2)
                {
                    // Protect lead - defensive formation
                    targetFormation = _defensiveFormation ?? _defaultFormation;
                }
            }

            // Apply formation if different
            if (targetFormation != null && targetFormation != _formationManager.CurrentFormation)
            {
                _formationManager.SetFormation(targetFormation);
                Debug.Log($"Coach: Formation changed to {targetFormation.formationName}");
            }
        }

        #endregion

        #region Tactical Decisions

        private void DecideTactics(GameContext context)
        {
            if (_teamTactics == null) return;

            // Defensive tactics
            DecideDefensiveTactics(context);

            // Offensive tactics
            DecideOffensiveTactics(context);
        }

        private void DecideDefensiveTactics(GameContext context)
        {
            // Already handled automatically by TeamTactics for numerical disadvantage

            // Additional strategic decisions
            if (context.isEndGame && context.isWinning && context.scoreDifference >= 3)
            {
                // Big lead, protect it - zone defense
                if (_tacticalFlexibility > 0.5f)
                {
                    _teamTactics.SetDefense(DefenseType.Zone, "Protecting lead");
                }
            }
            else if (context.isLosing && context.scoreDifference <= -3)
            {
                // Need to force turnovers - pressing defense
                if (_tacticalFlexibility > 0.6f && _riskTolerance > 0.5f)
                {
                    _teamTactics.SetDefense(DefenseType.Pressing, "Desperate to force turnovers");
                }
            }
        }

        private void DecideOffensiveTactics(GameContext context)
        {
            if (context.isEndGame && context.isLosing)
            {
                // Need to score quickly - fast break offense
                _teamTactics.SetOffense(OffenseType.FastBreak, "Need quick goals");
            }
            else if (context.isWinning && context.scoreDifference >= 2)
            {
                // Control the game - possession oriented
                _teamTactics.SetOffense(OffenseType.ThroughPivot, "Control possession");
            }
        }

        #endregion

        #region Substitutions

        private void ConsiderSubstitutions(GameContext context)
        {
            if (_teamManager == null) return;
            if (_substitutionFrequency < 0.1f) return; // Coach doesn't rotate much

            // Check if any substitutions needed
            // Phase 4 will have fatigue system, substitutions based on performance, etc.

            // For now, simplified: random rotation based on frequency
            if (Random.value < _substitutionFrequency * 0.01f) // Low probability per review
            {
                // Find a non-critical player to rest
                // This is very simplified for Phase 3
            }
        }

        #endregion

        #region Event Handlers

        private void OnQuarterEnded(QuarterEndedEvent evt)
        {
            // Review tactics at quarter break
            Debug.Log("Coach: Reviewing tactics at quarter break");
            ReviewTactics();

            // Consider substitutions for next quarter
            // Phase 4 will have rest/fatigue management
        }

        private void OnGoalScored(GoalScoredEvent evt)
        {
            // Immediate tactical review after significant event
            if (Random.value < _tacticalFlexibility)
            {
                Invoke(nameof(ReviewTactics), 2f); // Review after a short delay
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Manually trigger tactical review (for testing/debugging).
        /// </summary>
        [ContextMenu("Review Tactics Now")]
        public void ForceReviewTactics()
        {
            ReviewTactics();
        }

        #endregion
    }

    #region Helper Classes

    /// <summary>
    /// Represents current game context for tactical decisions.
    /// </summary>
    public class GameContext
    {
        public int scoreDifference = 0;
        public bool isWinning = false;
        public bool isLosing = false;
        public float quarterTimeRemaining = 0f;
        public int currentQuarter = 1;
        public bool isEndGame = false;
        public int fieldPlayerCount = 7;
        public bool isNumericallyDisadvantaged = false;
    }

    #endregion
}
