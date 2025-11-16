using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WaterPolo.Core;

namespace WaterPolo.UI
{
    /// <summary>
    /// Displays match score and time information.
    /// Can be used for physical scoreboard in world or UI canvas.
    /// </summary>
    public class ScoreboardDisplay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameClock _gameClock;
        [SerializeField] private ScoreTable _scoreTable;
        [SerializeField] private MatchState _matchState;

        [Header("UI Elements - Scores")]
        [SerializeField] private TextMeshProUGUI _homeTeamNameText;
        [SerializeField] private TextMeshProUGUI _awayTeamNameText;
        [SerializeField] private TextMeshProUGUI _homeScoreText;
        [SerializeField] private TextMeshProUGUI _awayScoreText;

        [Header("UI Elements - Time")]
        [SerializeField] private TextMeshProUGUI _quarterText;
        [SerializeField] private TextMeshProUGUI _matchTimeText;
        [SerializeField] private TextMeshProUGUI _shotClockText;

        [Header("UI Elements - Status")]
        [SerializeField] private TextMeshProUGUI _matchStatusText;
        [SerializeField] private GameObject _shotClockPanel;

        [Header("Colors")]
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _criticalColor = Color.red;
        [SerializeField] private float _shotClockCriticalThreshold = 5f;

        [Header("Update Rate")]
        [SerializeField] private float _updateInterval = 0.1f; // Update 10x per second

        private float _nextUpdateTime = 0f;

        #region Unity Lifecycle

        private void Awake()
        {
            // Find references if not assigned
            if (_gameClock == null)
                _gameClock = FindObjectOfType<GameClock>();

            if (_scoreTable == null)
                _scoreTable = FindObjectOfType<ScoreTable>();

            if (_matchState == null)
                _matchState = FindObjectOfType<MatchState>();

            // Subscribe to events
            EventBus.Instance.Subscribe<GoalScoredEvent>(OnGoalScored);
        }

        private void OnDestroy()
        {
            EventBus.Instance.Unsubscribe<GoalScoredEvent>(OnGoalScored);
        }

        private void Start()
        {
            // Initialize team names
            if (_scoreTable != null)
            {
                if (_homeTeamNameText != null)
                    _homeTeamNameText.text = _scoreTable.HomeTeamName;

                if (_awayTeamNameText != null)
                    _awayTeamNameText.text = _scoreTable.AwayTeamName;
            }

            // Initial update
            UpdateDisplay();
        }

        private void Update()
        {
            // Throttled updates
            if (Time.time >= _nextUpdateTime)
            {
                UpdateDisplay();
                _nextUpdateTime = Time.time + _updateInterval;
            }
        }

        #endregion

        #region Display Update

        private void UpdateDisplay()
        {
            UpdateScores();
            UpdateTime();
            UpdateStatus();
        }

        private void UpdateScores()
        {
            if (_scoreTable == null) return;

            if (_homeScoreText != null)
                _homeScoreText.text = _scoreTable.HomeScore.ToString();

            if (_awayScoreText != null)
                _awayScoreText.text = _scoreTable.AwayScore.ToString();
        }

        private void UpdateTime()
        {
            if (_gameClock == null) return;

            // Quarter
            if (_quarterText != null)
            {
                _quarterText.text = $"Q{_gameClock.CurrentQuarter}";
            }

            // Match time
            if (_matchTimeText != null)
            {
                float timeRemaining = _gameClock.QuarterTimeRemaining;
                int minutes = Mathf.FloorToInt(timeRemaining / 60f);
                int seconds = Mathf.FloorToInt(timeRemaining % 60f);
                _matchTimeText.text = $"{minutes:00}:{seconds:00}";

                // Color change when time is critical
                if (timeRemaining < 60f && _gameClock.CurrentQuarter == 4)
                {
                    _matchTimeText.color = _criticalColor;
                }
                else
                {
                    _matchTimeText.color = _normalColor;
                }
            }

            // Shot clock
            if (_shotClockText != null && _shotClockPanel != null)
            {
                if (_gameClock.ShotClockRunning)
                {
                    _shotClockPanel.SetActive(true);

                    float shotClockRemaining = _gameClock.ShotClockRemaining;
                    int seconds = Mathf.CeilToInt(shotClockRemaining);
                    _shotClockText.text = seconds.ToString();

                    // Color change when shot clock critical
                    if (shotClockRemaining < _shotClockCriticalThreshold)
                    {
                        _shotClockText.color = _criticalColor;
                    }
                    else
                    {
                        _shotClockText.color = _normalColor;
                    }
                }
                else
                {
                    _shotClockPanel.SetActive(false);
                }
            }
        }

        private void UpdateStatus()
        {
            if (_matchState == null || _matchStatusText == null) return;

            // Display match status
            string statusText = "";

            switch (_matchState.CurrentState)
            {
                case MatchStateType.PREGAME:
                    statusText = "PREGAME";
                    break;

                case MatchStateType.PLAYING:
                    statusText = ""; // No status during play
                    break;

                case MatchStateType.PAUSED:
                    statusText = "PAUSED";
                    break;

                case MatchStateType.GOAL_SCORED:
                    statusText = "GOAL!";
                    break;

                case MatchStateType.FOUL_CALLED:
                    statusText = "FOUL";
                    break;

                case MatchStateType.PENALTY:
                    statusText = "PENALTY";
                    break;

                case MatchStateType.QUARTER_END:
                    statusText = "QUARTER END";
                    break;

                case MatchStateType.POSTGAME:
                    if (_scoreTable != null)
                    {
                        statusText = $"FINAL - {_scoreTable.GetWinner()} WINS";
                    }
                    else
                    {
                        statusText = "FINAL";
                    }
                    break;
            }

            _matchStatusText.text = statusText;
        }

        #endregion

        #region Event Handlers

        private void OnGoalScored(GoalScoredEvent evt)
        {
            // Flash effect or animation could be added here
            // For now, standard update handles it
        }

        #endregion

        #region Public API

        /// <summary>
        /// Force immediate display update.
        /// </summary>
        public void ForceUpdate()
        {
            UpdateDisplay();
        }

        #endregion
    }
}
