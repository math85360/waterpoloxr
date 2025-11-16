using System;
using System.Collections.Generic;
using UnityEngine;

namespace WaterPolo.Core
{
    /// <summary>
    /// Central event bus for decoupled system communication.
    /// All game events are routed through this singleton.
    /// </summary>
    public class EventBus : MonoBehaviour
    {
        private static EventBus _instance;
        public static EventBus Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<EventBus>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("EventBus");
                        _instance = go.AddComponent<EventBus>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        private Dictionary<Type, Delegate> _eventDelegates = new Dictionary<Type, Delegate>();

        #region Subscribe/Unsubscribe

        /// <summary>
        /// Subscribe to an event type.
        /// </summary>
        public void Subscribe<T>(Action<T> listener) where T : GameEvent
        {
            Type eventType = typeof(T);

            if (_eventDelegates.ContainsKey(eventType))
            {
                _eventDelegates[eventType] = Delegate.Combine(_eventDelegates[eventType], listener);
            }
            else
            {
                _eventDelegates[eventType] = listener;
            }
        }

        /// <summary>
        /// Unsubscribe from an event type.
        /// </summary>
        public void Unsubscribe<T>(Action<T> listener) where T : GameEvent
        {
            Type eventType = typeof(T);

            if (_eventDelegates.ContainsKey(eventType))
            {
                _eventDelegates[eventType] = Delegate.Remove(_eventDelegates[eventType], listener);

                // Clean up if no more listeners
                if (_eventDelegates[eventType] == null)
                {
                    _eventDelegates.Remove(eventType);
                }
            }
        }

        #endregion

        #region Publish

        /// <summary>
        /// Publish an event to all subscribers.
        /// </summary>
        public void Publish<T>(T gameEvent) where T : GameEvent
        {
            Type eventType = typeof(T);

            if (_eventDelegates.ContainsKey(eventType))
            {
                Action<T> action = _eventDelegates[eventType] as Action<T>;
                action?.Invoke(gameEvent);
            }
        }

        #endregion

        #region Debug

        /// <summary>
        /// Get count of subscribers for a specific event type (debug only).
        /// </summary>
        public int GetSubscriberCount<T>() where T : GameEvent
        {
            Type eventType = typeof(T);
            if (_eventDelegates.ContainsKey(eventType))
            {
                return _eventDelegates[eventType]?.GetInvocationList().Length ?? 0;
            }
            return 0;
        }

        #endregion
    }

    #region Base Event Class

    /// <summary>
    /// Base class for all game events.
    /// </summary>
    public abstract class GameEvent
    {
        public float Timestamp { get; private set; }

        protected GameEvent()
        {
            Timestamp = Time.time;
        }
    }

    #endregion

    #region Core Events

    // Match State Events
    public class GamePausedEvent : GameEvent
    {
        public string Reason { get; private set; }
        public GamePausedEvent(string reason) { Reason = reason; }
    }

    public class GameResumedEvent : GameEvent { }

    public class QuarterEndedEvent : GameEvent
    {
        public int Quarter { get; private set; }
        public QuarterEndedEvent(int quarter) { Quarter = quarter; }
    }

    public class MatchEndedEvent : GameEvent
    {
        public string WinnerTeam { get; private set; }
        public MatchEndedEvent(string winner) { WinnerTeam = winner; }
    }

    // Scoring Events
    public class GoalScoredEvent : GameEvent
    {
        public string ScoringTeam { get; private set; }
        public MonoBehaviour Scorer { get; private set; }
        public int NewScore { get; private set; }

        public GoalScoredEvent(string team, MonoBehaviour scorer, int newScore)
        {
            ScoringTeam = team;
            Scorer = scorer;
            NewScore = newScore;
        }
    }

    public class GoalValidatedEvent : GameEvent
    {
        public MonoBehaviour Scorer { get; private set; }
        public GoalValidatedEvent(MonoBehaviour scorer) { Scorer = scorer; }
    }

    public class GoalInvalidatedEvent : GameEvent
    {
        public string Reason { get; private set; }
        public GoalInvalidatedEvent(string reason) { Reason = reason; }
    }

    // Clock Events
    public class ShotClockExpiredEvent : GameEvent { }

    public class ExclusionStartedEvent : GameEvent
    {
        public MonoBehaviour Player { get; private set; }
        public float Duration { get; private set; }

        public ExclusionStartedEvent(MonoBehaviour player, float duration)
        {
            Player = player;
            Duration = duration;
        }
    }

    public class ExclusionEndedEvent : GameEvent
    {
        public MonoBehaviour Player { get; private set; }
        public ExclusionEndedEvent(MonoBehaviour player) { Player = player; }
    }

    // Ball Events
    public class BallPossessionChangedEvent : GameEvent
    {
        public MonoBehaviour OldOwner { get; private set; }
        public MonoBehaviour NewOwner { get; private set; }

        public BallPossessionChangedEvent(MonoBehaviour oldOwner, MonoBehaviour newOwner)
        {
            OldOwner = oldOwner;
            NewOwner = newOwner;
        }
    }

    #endregion
}
