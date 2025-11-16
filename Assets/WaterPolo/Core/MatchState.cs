using UnityEngine;

namespace WaterPolo.Core
{
    /// <summary>
    /// Possible states of the water polo match.
    /// Controls game flow and which actions are allowed.
    /// </summary>
    public enum MatchStateType
    {
        PREGAME,           // Before match starts
        PLAYING,           // Active gameplay
        FOUL_CALLED,       // Referee has whistled
        FREE_THROW,        // Setting up for free throw
        PENALTY,           // Penalty shot (5m, goalie only)
        EXCLUSION,         // Repositioning after exclusion
        GOAL_SCORED,       // Celebration + reset
        QUARTER_END,       // Between quarters
        TIMEOUT,           // Team timeout
        PAUSED,            // General pause
        POSTGAME           // Match finished
    }

    /// <summary>
    /// Manages the current state of the match and transitions.
    /// Only one state is active at a time.
    /// </summary>
    public class MatchState : MonoBehaviour
    {
        [Header("Current State")]
        [SerializeField] private MatchStateType _currentState = MatchStateType.PREGAME;

        public MatchStateType CurrentState => _currentState;

        #region State Queries

        public bool IsPlaying => _currentState == MatchStateType.PLAYING;
        public bool IsPaused => _currentState == MatchStateType.PAUSED ||
                                 _currentState == MatchStateType.TIMEOUT ||
                                 _currentState == MatchStateType.QUARTER_END;
        public bool CanMove => _currentState == MatchStateType.PLAYING ||
                                _currentState == MatchStateType.FREE_THROW;
        public bool CanShoot => _currentState == MatchStateType.PLAYING ||
                                 _currentState == MatchStateType.FREE_THROW ||
                                 _currentState == MatchStateType.PENALTY;

        #endregion

        #region State Transitions

        /// <summary>
        /// Transition to a new match state.
        /// Validates transitions and publishes state change event.
        /// </summary>
        public void TransitionToState(MatchStateType newState)
        {
            if (_currentState == newState)
            {
                Debug.LogWarning($"Already in state {newState}");
                return;
            }

            // Validate transition
            if (!IsValidTransition(_currentState, newState))
            {
                Debug.LogError($"Invalid state transition from {_currentState} to {newState}");
                return;
            }

            MatchStateType previousState = _currentState;
            _currentState = newState;

            OnStateChanged(previousState, newState);

            Debug.Log($"Match state changed: {previousState} → {newState}");
        }

        /// <summary>
        /// Check if a state transition is valid.
        /// Some transitions are not allowed (e.g., POSTGAME → PLAYING).
        /// </summary>
        private bool IsValidTransition(MatchStateType from, MatchStateType to)
        {
            // Can't transition from POSTGAME
            if (from == MatchStateType.POSTGAME && to != MatchStateType.PREGAME)
                return false;

            // Can always pause
            if (to == MatchStateType.PAUSED || to == MatchStateType.TIMEOUT)
                return true;

            // Can always go to POSTGAME
            if (to == MatchStateType.POSTGAME)
                return true;

            // Otherwise, most transitions are valid for Phase 1
            return true;
        }

        /// <summary>
        /// Called when state changes. Override for custom behavior.
        /// </summary>
        protected virtual void OnStateChanged(MatchStateType previous, MatchStateType current)
        {
            // Handle state-specific logic
            switch (current)
            {
                case MatchStateType.PLAYING:
                    EventBus.Instance.Publish(new GameResumedEvent());
                    break;

                case MatchStateType.PAUSED:
                    EventBus.Instance.Publish(new GamePausedEvent("Manual pause"));
                    break;

                case MatchStateType.GOAL_SCORED:
                    // Goal event already published by ScoreTable
                    break;

                case MatchStateType.QUARTER_END:
                    // Quarter end event published by GameClock
                    break;

                case MatchStateType.POSTGAME:
                    // Match end event published by GameClock
                    break;
            }
        }

        #endregion

        #region Public API

        public void StartMatch()
        {
            TransitionToState(MatchStateType.PLAYING);
        }

        public void PauseMatch(string reason = "Manual pause")
        {
            TransitionToState(MatchStateType.PAUSED);
            EventBus.Instance.Publish(new GamePausedEvent(reason));
        }

        public void ResumeMatch()
        {
            TransitionToState(MatchStateType.PLAYING);
        }

        public void EndMatch(string winnerTeam)
        {
            TransitionToState(MatchStateType.POSTGAME);
            EventBus.Instance.Publish(new MatchEndedEvent(winnerTeam));
        }

        #endregion
    }
}
