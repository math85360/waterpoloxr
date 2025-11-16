using UnityEngine;
using WaterPolo.Core;

namespace WaterPolo.GameModes
{
    /// <summary>
    /// Abstract base class for all game modes.
    /// Defines common interface for different ways to play water polo.
    /// </summary>
    public abstract class GameMode : MonoBehaviour
    {
        [Header("Mode Info")]
        [SerializeField] protected string _modeName = "Game Mode";
        [SerializeField] protected string _description = "";

        [Header("Core References")]
        [SerializeField] protected GameClock _gameClock;
        [SerializeField] protected ScoreTable _scoreTable;
        [SerializeField] protected MatchState _matchState;

        protected bool _isActive = false;
        protected bool _isCompleted = false;

        #region Properties

        public string ModeName => _modeName;
        public string Description => _description;
        public bool IsActive => _isActive;
        public bool IsCompleted => _isCompleted;

        #endregion

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            // Find references if not assigned
            if (_gameClock == null)
                _gameClock = FindObjectOfType<GameClock>();

            if (_scoreTable == null)
                _scoreTable = FindObjectOfType<ScoreTable>();

            if (_matchState == null)
                _matchState = FindObjectOfType<MatchState>();
        }

        protected virtual void Update()
        {
            if (!_isActive) return;

            UpdateGameLogic();
        }

        #endregion

        #region Abstract Interface

        /// <summary>
        /// Setup the game mode (called before starting).
        /// </summary>
        public abstract void Setup();

        /// <summary>
        /// Start the game mode.
        /// </summary>
        public abstract void StartGame();

        /// <summary>
        /// Update game mode logic (called every frame when active).
        /// </summary>
        protected abstract void UpdateGameLogic();

        /// <summary>
        /// Check if win condition is met.
        /// </summary>
        public abstract bool CheckWinCondition();

        /// <summary>
        /// Handle goal scored (mode-specific behavior).
        /// </summary>
        public abstract void OnGoalScored(GoalScoredEvent goal);

        /// <summary>
        /// End the game mode.
        /// </summary>
        public abstract void EndGame();

        #endregion

        #region Common Methods

        protected virtual void Activate()
        {
            _isActive = true;
            _isCompleted = false;

            Debug.Log($"Game Mode Started: {_modeName}");
        }

        protected virtual void Deactivate()
        {
            _isActive = false;

            Debug.Log($"Game Mode Ended: {_modeName}");
        }

        protected virtual void Complete()
        {
            _isCompleted = true;
            Deactivate();

            Debug.Log($"Game Mode Completed: {_modeName}");
        }

        #endregion
    }
}
