using UnityEngine;
using WaterPolo.Core;

namespace WaterPolo.GameModes
{
    /// <summary>
    /// Target Practice mode.
    /// Shoot at goal zones for points. No opponents.
    /// Focus on accuracy and shot technique.
    /// </summary>
    public class TargetPracticeMode : GameMode
    {
        [Header("Mode Configuration")]
        [SerializeField] private int _shotLimit = 20;
        [SerializeField] private float _timeLimit = 300f; // 5 minutes
        [SerializeField] private bool _useTimeLimit = false;

        [Header("Scoring Zones")]
        [SerializeField] private int _cornerPoints = 10;      // Corners (hardest)
        [SerializeField] private int _sidePoints = 7;         // Sides
        [SerializeField] private int _centerPoints = 5;       // Center (easiest)

        [Header("Current State")]
        [SerializeField] private int _shotsTaken = 0;
        [SerializeField] private int _shotsHit = 0;
        [SerializeField] private int _totalPoints = 0;
        [SerializeField] private float _timeRemaining = 0f;

        [Header("Best Scores")]
        [SerializeField] private int _highScore = 0;

        #region Setup & Start

        public override void Setup()
        {
            _modeName = "Target Practice";
            _description = $"Shoot {_shotLimit} shots for maximum points";

            _shotsTaken = 0;
            _shotsHit = 0;
            _totalPoints = 0;
            _timeRemaining = _timeLimit;

            // Subscribe to events
            EventBus.Instance.Subscribe<GoalScoredEvent>(OnGoalScored);

            Debug.Log($"Target Practice setup: {_shotLimit} shots");
        }

        public override void StartGame()
        {
            Activate();

            if (_matchState != null)
            {
                _matchState.TransitionToState(MatchStateType.PLAYING);
            }

            Debug.Log("Target Practice started - aim for the corners!");
        }

        #endregion

        #region Game Logic

        protected override void UpdateGameLogic()
        {
            if (_useTimeLimit)
            {
                _timeRemaining -= Time.deltaTime;

                if (_timeRemaining <= 0f)
                {
                    EndGame();
                }
            }

            // Check shot limit
            if (_shotsTaken >= _shotLimit)
            {
                EndGame();
            }
        }

        public override bool CheckWinCondition()
        {
            // Win condition: complete all shots or time expires
            return _shotsTaken >= _shotLimit || (_useTimeLimit && _timeRemaining <= 0f);
        }

        public override void OnGoalScored(GoalScoredEvent goal)
        {
            // Determine which zone was hit
            // For full implementation, would check ball position on goal
            // Simplified: random zone for Phase 5

            int points = DetermineZonePoints(goal);
            _totalPoints += points;
            _shotsHit++;

            Debug.Log($"Hit! +{points} points. Total: {_totalPoints}");

            UpdateHighScore();
        }

        /// <summary>
        /// Register a shot attempt (hit or miss).
        /// </summary>
        public void RegisterShot(bool hit, Vector3 ballPosition = default)
        {
            _shotsTaken++;

            if (!hit)
            {
                Debug.Log($"Miss! Shots: {_shotsTaken}/{_shotLimit}");
            }

            // Display progress
            float accuracy = _shotsTaken > 0 ? (_shotsHit / (float)_shotsTaken) * 100f : 0f;
            Debug.Log($"Accuracy: {accuracy:F1}% ({_shotsHit}/{_shotsTaken})");
        }

        private int DetermineZonePoints(GoalScoredEvent goal)
        {
            // Simplified zone determination
            // Full implementation would check ball impact position

            // Random for Phase 5
            float roll = Random.value;

            if (roll < 0.3f)
                return _cornerPoints; // 30% corner hits

            if (roll < 0.6f)
                return _sidePoints; // 30% side hits

            return _centerPoints; // 40% center hits
        }

        private void UpdateHighScore()
        {
            if (_totalPoints > _highScore)
            {
                _highScore = _totalPoints;
                Debug.Log($"NEW HIGH SCORE: {_highScore}!");
            }
        }

        #endregion

        #region End Game

        public override void EndGame()
        {
            if (_isCompleted) return;

            Complete();

            EventBus.Instance.Unsubscribe<GoalScoredEvent>(OnGoalScored);

            // Calculate final stats
            float accuracy = _shotsTaken > 0 ? (_shotsHit / (float)_shotsTaken) * 100f : 0f;
            float avgPointsPerShot = _shotsTaken > 0 ? _totalPoints / (float)_shotsTaken : 0f;

            Debug.Log($"=== TARGET PRACTICE COMPLETE ===");
            Debug.Log($"Total Points: {_totalPoints}");
            Debug.Log($"Shots: {_shotsHit}/{_shotsTaken}");
            Debug.Log($"Accuracy: {accuracy:F1}%");
            Debug.Log($"Avg Points/Shot: {avgPointsPerShot:F1}");
            Debug.Log($"High Score: {_highScore}");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get current statistics.
        /// </summary>
        public (int points, int shots, int hits, float accuracy) GetStats()
        {
            float accuracy = _shotsTaken > 0 ? (_shotsHit / (float)_shotsTaken) * 100f : 0f;
            return (_totalPoints, _shotsTaken, _shotsHit, accuracy);
        }

        #endregion
    }
}
