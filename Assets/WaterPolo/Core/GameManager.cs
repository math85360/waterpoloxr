using UnityEngine;

namespace WaterPolo.Core
{
    /// <summary>
    /// Central game manager that coordinates all core systems.
    /// Handles match initialization, flow control, and system coordination.
    /// Phase 1: Basic match management.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Core Systems")]
        [SerializeField] private GameClock _gameClock;
        [SerializeField] private MatchState _matchState;
        [SerializeField] private ScoreTable _scoreTable;
        [SerializeField] private EventBus _eventBus;

        [Header("Match Configuration")]
        [SerializeField] private bool _autoStartMatch = false;
        [SerializeField] private float _matchStartDelay = 3f;

        [Header("Ball")]
        [SerializeField] private GameObject _ball;
        [SerializeField] private Vector3 _ballStartPosition = Vector3.zero;

        private bool _isInitialized = false;

        #region Unity Lifecycle

        private void Awake()
        {
            // Find core systems if not assigned
            if (_gameClock == null)
                _gameClock = GetComponent<GameClock>();

            if (_matchState == null)
                _matchState = GetComponent<MatchState>();

            if (_scoreTable == null)
                _scoreTable = GetComponent<ScoreTable>();

            if (_eventBus == null)
                _eventBus = EventBus.Instance;

            // Find ball if not assigned
            if (_ball == null)
                _ball = GameObject.FindGameObjectWithTag("Ball");

            ValidateSystems();
        }

        private void Start()
        {
            Initialize();

            if (_autoStartMatch)
            {
                Invoke(nameof(StartMatch), _matchStartDelay);
            }
        }

        #endregion

        #region Initialization

        private void ValidateSystems()
        {
            bool allSystemsPresent = true;

            if (_gameClock == null)
            {
                Debug.LogError("GameManager: GameClock not found!");
                allSystemsPresent = false;
            }

            if (_matchState == null)
            {
                Debug.LogError("GameManager: MatchState not found!");
                allSystemsPresent = false;
            }

            if (_scoreTable == null)
            {
                Debug.LogError("GameManager: ScoreTable not found!");
                allSystemsPresent = false;
            }

            if (_ball == null)
            {
                Debug.LogWarning("GameManager: Ball not found! Make sure ball has tag 'Ball'");
            }

            _isInitialized = allSystemsPresent;
        }

        private void Initialize()
        {
            if (!_isInitialized)
            {
                Debug.LogError("GameManager: Cannot initialize - missing core systems!");
                return;
            }

            // Subscribe to events
            _eventBus.Subscribe<GoalScoredEvent>(OnGoalScored);
            _eventBus.Subscribe<QuarterEndedEvent>(OnQuarterEnded);
            _eventBus.Subscribe<MatchEndedEvent>(OnMatchEnded);
            _eventBus.Subscribe<ShotClockExpiredEvent>(OnShotClockExpired);

            Debug.Log("GameManager initialized successfully");
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (_eventBus != null)
            {
                _eventBus.Unsubscribe<GoalScoredEvent>(OnGoalScored);
                _eventBus.Unsubscribe<QuarterEndedEvent>(OnQuarterEnded);
                _eventBus.Unsubscribe<MatchEndedEvent>(OnMatchEnded);
                _eventBus.Unsubscribe<ShotClockExpiredEvent>(OnShotClockExpired);
            }
        }

        #endregion

        #region Match Control

        public void StartMatch()
        {
            if (!_isInitialized)
            {
                Debug.LogError("Cannot start match - GameManager not initialized!");
                return;
            }

            Debug.Log("=== MATCH STARTING ===");

            // Reset all systems
            ResetMatch();

            // Position ball at center
            if (_ball != null)
            {
                _ball.transform.position = _ballStartPosition;
                Ball.BallController ballController = _ball.GetComponent<Ball.BallController>();
                if (ballController != null)
                {
                    ballController.ResetBall(_ballStartPosition);
                }
            }

            // Start match state
            _matchState.StartMatch();

            // Start clocks
            _gameClock.StartClock();

            Debug.Log("Match started!");
        }

        public void PauseMatch()
        {
            if (_matchState != null)
            {
                _matchState.PauseMatch("Manual pause");
            }

            if (_gameClock != null)
            {
                _gameClock.StopClock();
            }

            Debug.Log("Match paused");
        }

        public void ResumeMatch()
        {
            if (_matchState != null)
            {
                _matchState.ResumeMatch();
            }

            if (_gameClock != null)
            {
                _gameClock.StartClock();
            }

            Debug.Log("Match resumed");
        }

        public void EndMatch()
        {
            if (_gameClock != null)
            {
                _gameClock.StopClock();
            }

            if (_scoreTable != null)
            {
                string winner = _scoreTable.GetWinner();
                _matchState.EndMatch(winner);

                Debug.Log($"=== MATCH ENDED ===");
                Debug.Log($"Final Score: {_scoreTable.HomeTeamName} {_scoreTable.HomeScore} - {_scoreTable.AwayScore} {_scoreTable.AwayTeamName}");
                Debug.Log($"Winner: {winner}");
            }
        }

        public void ResetMatch()
        {
            if (_gameClock != null)
            {
                _gameClock.ResetMatch();
            }

            if (_scoreTable != null)
            {
                _scoreTable.ResetScore();
            }

            Debug.Log("Match reset");
        }

        #endregion

        #region Event Handlers

        private void OnGoalScored(GoalScoredEvent evt)
        {
            Debug.Log($"[GameManager] Goal scored by {evt.ScoringTeam}! New score: {evt.NewScore}");

            // Stop clocks
            _gameClock.StopClock();

            // Wait a few seconds then reset for kickoff
            Invoke(nameof(ResetAfterGoal), 3f);
        }

        private void ResetAfterGoal()
        {
            // Reset ball to center
            if (_ball != null)
            {
                _ball.transform.position = _ballStartPosition;
                Ball.BallController ballController = _ball.GetComponent<Ball.BallController>();
                if (ballController != null)
                {
                    ballController.ResetBall(_ballStartPosition);
                }
            }

            // Resume play
            _matchState.TransitionToState(MatchStateType.PLAYING);
            _gameClock.StartClock();
            _gameClock.ResetShotClock();

            Debug.Log("Play resumed after goal");
        }

        private void OnQuarterEnded(QuarterEndedEvent evt)
        {
            Debug.Log($"[GameManager] Quarter {evt.Quarter} ended");

            // Check if match is over
            if (evt.Quarter >= 4)
            {
                EndMatch();
            }
            else
            {
                // Prepare for next quarter
                Invoke(nameof(StartNextQuarter), 5f);
            }
        }

        private void StartNextQuarter()
        {
            _gameClock.StartNextQuarter();
            Debug.Log($"Quarter {_gameClock.CurrentQuarter} started");
        }

        private void OnMatchEnded(MatchEndedEvent evt)
        {
            Debug.Log($"[GameManager] Match ended - Winner: {evt.WinnerTeam}");
        }

        private void OnShotClockExpired(ShotClockExpiredEvent evt)
        {
            Debug.Log("[GameManager] Shot clock expired - Turnover!");

            // Force turnover - ball released from current owner
            if (_ball != null)
            {
                Ball.BallController ballController = _ball.GetComponent<Ball.BallController>();
                if (ballController != null)
                {
                    ballController.ForceTurnover();
                }
            }

            // Reset shot clock for the new team
            _gameClock.ResetShotClock();
        }

        #endregion

        #region Public API

        public GameClock GetGameClock() => _gameClock;
        public MatchState GetMatchState() => _matchState;
        public ScoreTable GetScoreTable() => _scoreTable;

        public bool IsMatchRunning()
        {
            return _matchState != null && _matchState.IsPlaying;
        }

        #endregion

        #region Debug

        [ContextMenu("Start Match")]
        public void DebugStartMatch()
        {
            StartMatch();
        }

        [ContextMenu("Pause Match")]
        public void DebugPauseMatch()
        {
            PauseMatch();
        }

        [ContextMenu("Resume Match")]
        public void DebugResumeMatch()
        {
            ResumeMatch();
        }

        [ContextMenu("End Match")]
        public void DebugEndMatch()
        {
            EndMatch();
        }

        [ContextMenu("Score Goal (Home)")]
        public void DebugScoreGoalHome()
        {
            _scoreTable?.RegisterGoal(_scoreTable.HomeTeamName, null);
        }

        [ContextMenu("Score Goal (Away)")]
        public void DebugScoreGoalAway()
        {
            _scoreTable?.RegisterGoal(_scoreTable.AwayTeamName, null);
        }

        #endregion
    }
}
