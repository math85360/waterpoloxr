using System.Collections.Generic;
using UnityEngine;

namespace WaterPolo.Core
{
    /// <summary>
    /// Manages all match timing: quarters, shot clock, and exclusion timers.
    /// All clocks are independent and can run simultaneously.
    /// </summary>
    public class GameClock : MonoBehaviour
    {
        [Header("Match Configuration")]
        [SerializeField] private int _quarterCount = 4;
        [SerializeField] private float _quarterDuration = 480f; // 8 minutes

        [Header("Shot Clock")]
        [SerializeField] private bool _useShotClock = true;
        [SerializeField] private float _shotClockDuration = 30f;
        [SerializeField] private float _exclusionShotClock = 20f; // When team has numerical advantage

        [Header("Current State")]
        [SerializeField] private int _currentQuarter = 1;
        [SerializeField] private float _quarterTimeRemaining = 480f;
        [SerializeField] private float _shotClockRemaining = 30f;
        [SerializeField] private bool _isRunning = false;
        [SerializeField] private bool _shotClockRunning = false;

        private List<ExclusionClock> _activeExclusions = new List<ExclusionClock>();
        private MatchState _matchState;

        #region Properties

        public int CurrentQuarter => _currentQuarter;
        public float QuarterTimeRemaining => _quarterTimeRemaining;
        public float ShotClockRemaining => _shotClockRemaining;
        public bool IsRunning => _isRunning;
        public bool ShotClockRunning => _shotClockRunning;
        public int ActiveExclusionCount => _activeExclusions.Count;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _matchState = GetComponent<MatchState>();
            if (_matchState == null)
            {
                Debug.LogError("GameClock requires MatchState component on same GameObject!");
            }
        }

        private void Update()
        {
            if (!_isRunning) return;

            float deltaTime = Time.deltaTime;

            // Update quarter time
            UpdateQuarterTime(deltaTime);

            // Update shot clock
            if (_shotClockRunning)
            {
                UpdateShotClock(deltaTime);
            }

            // Update all active exclusion clocks
            UpdateExclusionClocks(deltaTime);
        }

        #endregion

        #region Quarter Time

        private void UpdateQuarterTime(float deltaTime)
        {
            _quarterTimeRemaining -= deltaTime;

            if (_quarterTimeRemaining <= 0f)
            {
                OnQuarterEnd();
            }
        }

        private void OnQuarterEnd()
        {
            _isRunning = false;
            _shotClockRunning = false;

            EventBus.Instance.Publish(new QuarterEndedEvent(_currentQuarter));

            if (_currentQuarter >= _quarterCount)
            {
                // Match is over
                _matchState.EndMatch("TBD"); // Score table will determine winner
            }
            else
            {
                // Prepare for next quarter
                _matchState.TransitionToState(MatchStateType.QUARTER_END);
            }

            Debug.Log($"Quarter {_currentQuarter} ended");
        }

        public void StartNextQuarter()
        {
            if (_currentQuarter >= _quarterCount)
            {
                Debug.LogWarning("Cannot start next quarter - match is over");
                return;
            }

            _currentQuarter++;
            _quarterTimeRemaining = _quarterDuration;
            ResetShotClock();

            _matchState.TransitionToState(MatchStateType.PLAYING);
            _isRunning = true;

            Debug.Log($"Quarter {_currentQuarter} started");
        }

        #endregion

        #region Shot Clock

        private void UpdateShotClock(float deltaTime)
        {
            _shotClockRemaining -= deltaTime;

            if (_shotClockRemaining <= 0f)
            {
                OnShotClockExpired();
            }
        }

        private void OnShotClockExpired()
        {
            _shotClockRunning = false;
            EventBus.Instance.Publish(new ShotClockExpiredEvent());

            Debug.Log("Shot clock expired - turnover");
        }

        public void ResetShotClock(bool exclusionAdvantage = false)
        {
            if (!_useShotClock) return;

            _shotClockRemaining = exclusionAdvantage ? _exclusionShotClock : _shotClockDuration;
            _shotClockRunning = true;
        }

        public void StartShotClock()
        {
            if (!_useShotClock) return;
            _shotClockRunning = true;
        }

        public void StopShotClock()
        {
            _shotClockRunning = false;
        }

        #endregion

        #region Exclusion Clocks

        /// <summary>
        /// Start a new exclusion timer for a player.
        /// </summary>
        public ExclusionClock StartExclusion(MonoBehaviour player, float duration = 20f)
        {
            ExclusionClock exclusion = new ExclusionClock(player, duration);
            _activeExclusions.Add(exclusion);

            EventBus.Instance.Publish(new ExclusionStartedEvent(player, duration));

            Debug.Log($"Exclusion started for {player.name} - {duration}s");

            return exclusion;
        }

        private void UpdateExclusionClocks(float deltaTime)
        {
            for (int i = _activeExclusions.Count - 1; i >= 0; i--)
            {
                _activeExclusions[i].TimeRemaining -= deltaTime;

                if (_activeExclusions[i].TimeRemaining <= 0f)
                {
                    OnExclusionEnd(_activeExclusions[i]);
                    _activeExclusions.RemoveAt(i);
                }
            }
        }

        private void OnExclusionEnd(ExclusionClock exclusion)
        {
            EventBus.Instance.Publish(new ExclusionEndedEvent(exclusion.Player));
            Debug.Log($"Exclusion ended for {exclusion.Player.name}");
        }

        public List<ExclusionClock> GetActiveExclusions()
        {
            return new List<ExclusionClock>(_activeExclusions);
        }

        #endregion

        #region Control

        public void StartClock()
        {
            _isRunning = true;
            if (_useShotClock)
            {
                _shotClockRunning = true;
            }
        }

        public void StopClock()
        {
            _isRunning = false;
            _shotClockRunning = false;
        }

        public void ResetMatch()
        {
            _currentQuarter = 1;
            _quarterTimeRemaining = _quarterDuration;
            _shotClockRemaining = _shotClockDuration;
            _isRunning = false;
            _shotClockRunning = false;
            _activeExclusions.Clear();
        }

        #endregion
    }

    /// <summary>
    /// Represents an individual exclusion timer (20 seconds).
    /// Independent from match time and shot clock.
    /// </summary>
    [System.Serializable]
    public class ExclusionClock
    {
        public MonoBehaviour Player { get; private set; }
        public float TimeRemaining { get; set; }
        public float InitialDuration { get; private set; }

        public ExclusionClock(MonoBehaviour player, float duration)
        {
            Player = player;
            TimeRemaining = duration;
            InitialDuration = duration;
        }

        public float GetProgress()
        {
            return 1f - (TimeRemaining / InitialDuration);
        }
    }
}
