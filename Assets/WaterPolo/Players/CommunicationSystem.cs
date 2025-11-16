using UnityEngine;
using System.Collections.Generic;
using WaterPolo.Core;

namespace WaterPolo.Players
{
    /// <summary>
    /// Types of calls/communication between players.
    /// </summary>
    public enum CallType
    {
        RequestBall,    // "Ballon !" - I'm open
        ImOpen,         // "Je suis seul !" - Emphatic
        Screen,         // "Écran !" - Setting screen
        Switch,         // "Change !" - Switch defensive marks
        Shot,           // "Tire !" - Encouragement to shoot
        Time,           // "Temps !" - Shot clock warning
        Defense,        // "Reviens !" - Fall back on defense
        Help,           // "Aide !" - Need defensive help
        Cut,            // "Coupe !" - Cutting to goal
        Post            // "Poste !" - Posting up
    }

    /// <summary>
    /// Represents a communication call from a player.
    /// </summary>
    public class PlayerCall
    {
        public WaterPoloPlayer Caller { get; private set; }
        public CallType Type { get; private set; }
        public WaterPoloPlayer Target { get; private set; } // Null = broadcast to team
        public Vector3 Position { get; private set; }
        public float Timestamp { get; private set; }
        public float Urgency { get; private set; } // 0-1

        public PlayerCall(WaterPoloPlayer caller, CallType type, WaterPoloPlayer target, float urgency)
        {
            Caller = caller;
            Type = type;
            Target = target;
            Position = caller.transform.position;
            Timestamp = Time.time;
            Urgency = Mathf.Clamp01(urgency);
        }

        public bool IsExpired(float maxAge = 2f)
        {
            return (Time.time - Timestamp) > maxAge;
        }
    }

    /// <summary>
    /// Manages communication between players on a team.
    /// Handles call generation, audio, and visual indicators.
    /// </summary>
    public class CommunicationSystem : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool _enableAutoCalls = true;
        [SerializeField] private float _callCooldown = 2f; // Min time between calls from same player
        [SerializeField] private float _callMaxAge = 3f; // How long calls stay active

        [Header("Audio")]
        [SerializeField] private bool _enableAudio = true;
        [SerializeField] private AudioSource _audioSourcePrefab;
        [SerializeField] private float _audioMaxDistance = 15f;

        [Header("Visual")]
        [SerializeField] private bool _enableVisualIndicators = true;
        [SerializeField] private GameObject _callIndicatorPrefab;

        [Header("Ball Context")]
        [SerializeField] private GameObject _ball;
        [SerializeField] private WaterPoloPlayer _ballCarrier;

        private List<PlayerCall> _activeCalls = new List<PlayerCall>();
        private Dictionary<WaterPoloPlayer, float> _lastCallTime = new Dictionary<WaterPoloPlayer, float>();
        private Dictionary<PlayerCall, GameObject> _visualIndicators = new Dictionary<PlayerCall, GameObject>();

        #region Unity Lifecycle

        private void Awake()
        {
            // Find ball if not assigned
            if (_ball == null)
            {
                _ball = GameObject.FindGameObjectWithTag("Ball");
            }

            // Subscribe to events
            EventBus.Instance.Subscribe<BallPossessionChangedEvent>(OnBallPossessionChanged);
        }

        private void OnDestroy()
        {
            EventBus.Instance.Unsubscribe<BallPossessionChangedEvent>(OnBallPossessionChanged);

            // Clean up visual indicators
            foreach (var indicator in _visualIndicators.Values)
            {
                if (indicator != null)
                    Destroy(indicator);
            }
        }

        private void Update()
        {
            // Clean up expired calls
            _activeCalls.RemoveAll(call => call.IsExpired(_callMaxAge));

            // Remove expired visual indicators
            List<PlayerCall> expiredCalls = new List<PlayerCall>();
            foreach (var kvp in _visualIndicators)
            {
                if (kvp.Key.IsExpired(_callMaxAge))
                {
                    expiredCalls.Add(kvp.Key);
                    if (kvp.Value != null)
                        Destroy(kvp.Value);
                }
            }
            foreach (var call in expiredCalls)
            {
                _visualIndicators.Remove(call);
            }

            // Auto-generate calls based on game context
            if (_enableAutoCalls)
            {
                GenerateAutoCalls();
            }
        }

        #endregion

        #region Making Calls

        /// <summary>
        /// Make a call from a player.
        /// </summary>
        public void MakeCall(WaterPoloPlayer caller, CallType type, WaterPoloPlayer target = null, float urgency = 0.5f)
        {
            if (caller == null) return;

            // Check cooldown
            if (_lastCallTime.ContainsKey(caller))
            {
                if (Time.time - _lastCallTime[caller] < _callCooldown)
                {
                    return; // Too soon
                }
            }

            // Create call
            PlayerCall call = new PlayerCall(caller, type, target, urgency);
            _activeCalls.Add(call);
            _lastCallTime[caller] = Time.time;

            // Handle call (audio, visual, etc.)
            ProcessCall(call);

            // Publish event
            EventBus.Instance.Publish(new BallCallMadeEvent(caller, type, target));

            Debug.Log($"{caller.PlayerName} calls: {type} (urgency: {urgency:F2})");
        }

        private void ProcessCall(PlayerCall call)
        {
            // Play audio
            if (_enableAudio)
            {
                PlayCallAudio(call);
            }

            // Show visual indicator
            if (_enableVisualIndicators)
            {
                ShowCallIndicator(call);
            }
        }

        #endregion

        #region Auto Call Generation

        private void GenerateAutoCalls()
        {
            // Generate calls based on game context
            // This runs every frame, but calls are rate-limited by cooldown

            // Example: Request ball when open
            // Phase 4 will have more sophisticated call generation
        }

        /// <summary>
        /// Check if player should call for ball (AI decision support).
        /// </summary>
        public bool ShouldCallForBall(WaterPoloPlayer player)
        {
            if (_ballCarrier == null || _ballCarrier == player)
                return false;

            // Don't call if not on same team
            if (_ballCarrier.TeamName != player.TeamName)
                return false;

            // Check if in good position
            // Simplified for Phase 3

            // Check if ball carrier can see you
            Vector3 toBallCarrier = _ballCarrier.transform.position - player.transform.position;
            Vector3 ballCarrierForward = _ballCarrier.transform.forward;

            float angle = Vector3.Angle(ballCarrierForward, toBallCarrier);

            // If behind ball carrier or far to the side, call for ball
            if (angle > 90f)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get urgency level for a time warning call.
        /// </summary>
        public float GetTimeWarningUrgency(float shotClockRemaining)
        {
            if (shotClockRemaining < 3f)
                return 1f; // Critical

            if (shotClockRemaining < 5f)
                return 0.7f; // High

            if (shotClockRemaining < 10f)
                return 0.4f; // Medium

            return 0f; // No urgency
        }

        #endregion

        #region Audio

        private void PlayCallAudio(PlayerCall call)
        {
            if (_audioSourcePrefab == null) return;

            // Create 3D spatial audio at caller's position
            AudioSource audioSource = Instantiate(_audioSourcePrefab, call.Position, Quaternion.identity);

            // Configure spatial audio
            audioSource.spatialBlend = 1f; // Full 3D
            audioSource.maxDistance = _audioMaxDistance;
            audioSource.rolloffMode = AudioRolloffMode.Linear;

            // Select audio clip based on call type
            // Phase 4 will have actual audio clips
            // For now, just create the audio source

            // Play and destroy after clip finishes
            audioSource.Play();
            Destroy(audioSource.gameObject, 2f);
        }

        #endregion

        #region Visual Indicators

        private void ShowCallIndicator(PlayerCall call)
        {
            if (_callIndicatorPrefab == null)
            {
                // Create simple indicator if no prefab
                CreateSimpleIndicator(call);
                return;
            }

            // Instantiate indicator above caller
            Vector3 indicatorPos = call.Position + Vector3.up * 1.5f;
            GameObject indicator = Instantiate(_callIndicatorPrefab, indicatorPos, Quaternion.identity);

            // Parent to caller so it follows
            indicator.transform.SetParent(call.Caller.transform);

            // Set color based on urgency
            Color color = Color.Lerp(Color.green, Color.red, call.Urgency);
            Renderer renderer = indicator.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }

            _visualIndicators[call] = indicator;
        }

        private void CreateSimpleIndicator(PlayerCall call)
        {
            // Create simple sphere indicator
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicator.transform.position = call.Position + Vector3.up * 1.5f;
            indicator.transform.localScale = Vector3.one * 0.3f;
            indicator.transform.SetParent(call.Caller.transform);

            // Color based on urgency
            Color color = Color.Lerp(Color.green, Color.red, call.Urgency);
            Renderer renderer = indicator.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = color;
                renderer.material = mat;
            }

            // Remove collider
            Collider collider = indicator.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            _visualIndicators[call] = indicator;
        }

        #endregion

        #region Queries

        /// <summary>
        /// Get all active calls for a team.
        /// </summary>
        public List<PlayerCall> GetActiveCalls(string teamName)
        {
            return _activeCalls.FindAll(call => call.Caller.TeamName == teamName);
        }

        /// <summary>
        /// Get most urgent active call.
        /// </summary>
        public PlayerCall GetMostUrgentCall(string teamName)
        {
            List<PlayerCall> teamCalls = GetActiveCalls(teamName);

            if (teamCalls.Count == 0)
                return null;

            PlayerCall mostUrgent = teamCalls[0];
            foreach (var call in teamCalls)
            {
                if (call.Urgency > mostUrgent.Urgency)
                    mostUrgent = call;
            }

            return mostUrgent;
        }

        #endregion

        #region Event Handlers

        private void OnBallPossessionChanged(BallPossessionChangedEvent evt)
        {
            _ballCarrier = evt.NewOwner as WaterPoloPlayer;
        }

        #endregion
    }

    #region Event Classes

    public class BallCallMadeEvent : GameEvent
    {
        public WaterPoloPlayer Caller { get; private set; }
        public CallType Type { get; private set; }
        public WaterPoloPlayer Target { get; private set; }

        public BallCallMadeEvent(WaterPoloPlayer caller, CallType type, WaterPoloPlayer target)
        {
            Caller = caller;
            Type = type;
            Target = target;
        }
    }

    #endregion
}
