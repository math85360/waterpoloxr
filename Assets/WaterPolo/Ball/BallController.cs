using UnityEngine;
using WaterPolo.Core;
using WaterPolo.Players;

namespace WaterPolo.Ball
{
    /// <summary>
    /// Ball states as defined in CLAUDE.md architecture.
    /// </summary>
    public enum BallState
    {
        FREE,       // Ball is loose in the water (full physics)
        POSSESSED,  // Ball is held by a player (physics disabled)
        PASSING,    // Ball is in flight during a pass
        SHOOTING,   // Ball is in flight during a shot
        BOUNCING    // Ball is bouncing off a surface
    }

    /// <summary>
    /// Manages the water polo ball state and physics.
    /// Integrates with existing BallBuoyancy for water physics.
    /// Handles possession, passing, and shooting mechanics.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class BallController : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private BallState _currentState = BallState.FREE;
        [SerializeField] private WaterPoloPlayer _currentOwner = null;

        [Header("Physics")]
        [SerializeField] private bool _usePhysics = true;

        [Header("Possession")]
        [SerializeField] private float _possessionRadius = 0.5f;
        [SerializeField] private Transform _attachPoint = null; // Where ball attaches to player hand

        [Header("Throwing")]
        [SerializeField] private float _passForceMultiplier = 1.0f;
        [SerializeField] private float _shootForceMultiplier = 1.5f;

        private Rigidbody _rigidbody;
        private BallBuoyancy _buoyancy; // Existing buoyancy script

        #region Properties

        public BallState CurrentState => _currentState;
        public WaterPoloPlayer CurrentOwner => _currentOwner;
        public bool IsFree => _currentState == BallState.FREE;
        public bool IsPossessed => _currentState == BallState.POSSESSED;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _buoyancy = GetComponent<BallBuoyancy>();

            if (_rigidbody == null)
            {
                Debug.LogError("BallController requires Rigidbody component!");
            }
        }

        private void Update()
        {
            // Update based on current state
            switch (_currentState)
            {
                case BallState.POSSESSED:
                    UpdatePossessionState();
                    break;

                case BallState.FREE:
                    CheckForPossession();
                    break;

                case BallState.PASSING:
                case BallState.SHOOTING:
                    // Let physics handle it
                    // Could add trajectory assistance here in future
                    break;
            }
        }

        #endregion

        #region State Transitions

        private void TransitionToState(BallState newState)
        {
            if (_currentState == newState) return;

            BallState previousState = _currentState;
            _currentState = newState;

            OnStateChanged(previousState, newState);
        }

        private void OnStateChanged(BallState previous, BallState current)
        {
            // Handle state-specific setup
            switch (current)
            {
                case BallState.FREE:
                    EnablePhysics();
                    _attachPoint = null;
                    break;

                case BallState.POSSESSED:
                    DisablePhysics();
                    break;

                case BallState.PASSING:
                case BallState.SHOOTING:
                    EnablePhysics();
                    _attachPoint = null;
                    break;

                case BallState.BOUNCING:
                    EnablePhysics();
                    break;
            }

            Debug.Log($"Ball state: {previous} → {current}");
        }

        #endregion

        #region Physics Control

        private void EnablePhysics()
        {
            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = false;
                _rigidbody.useGravity = true;
            }

            if (_buoyancy != null)
            {
                _buoyancy.enabled = true;
            }
        }

        private void DisablePhysics()
        {
            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = true;
                _rigidbody.velocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }

            if (_buoyancy != null)
            {
                _buoyancy.enabled = false;
            }
        }

        #endregion

        #region Possession

        private void CheckForPossession()
        {
            // Find nearby players
            WaterPoloPlayer[] players = FindObjectsOfType<WaterPoloPlayer>();

            foreach (WaterPoloPlayer player in players)
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);

                if (distance < _possessionRadius)
                {
                    // Player can take possession
                    // In Phase 1, we auto-assign. In Phase 2+, player must trigger grab action
                    TryGivePossession(player);
                    break;
                }
            }
        }

        public bool TryGivePossession(WaterPoloPlayer player)
        {
            if (_currentState == BallState.POSSESSED)
            {
                // Already possessed by someone
                return false;
            }

            _currentOwner = player;
            TransitionToState(BallState.POSSESSED);

            // Notify player
            player.TakePossession(gameObject);

            // Publish event
            EventBus.Instance.Publish(new BallPossessionChangedEvent(null, player));

            Debug.Log($"Ball possessed by {player.PlayerName}");

            return true;
        }

        private void UpdatePossessionState()
        {
            if (_currentOwner == null)
            {
                // Lost owner, return to free
                TransitionToState(BallState.FREE);
                return;
            }

            // Attach ball to player's hand
            Vector3 handPosition = _currentOwner.GetArmPosition(ArmSide.Right);
            transform.position = handPosition;

            // Match hand rotation (optional)
            // transform.rotation = _currentOwner.GetHandRotation(ArmSide.Right);
        }

        #endregion

        #region Throwing

        public void ReleaseBall(Vector3 velocity, bool isShot = false)
        {
            if (_currentState != BallState.POSSESSED)
            {
                Debug.LogWarning("Cannot release ball - not possessed!");
                return;
            }

            WaterPoloPlayer previousOwner = _currentOwner;
            _currentOwner = null;

            // Transition state
            TransitionToState(isShot ? BallState.SHOOTING : BallState.PASSING);

            // Apply velocity
            if (_rigidbody != null)
            {
                float forceMultiplier = isShot ? _shootForceMultiplier : _passForceMultiplier;
                _rigidbody.velocity = velocity * forceMultiplier;
            }

            // Notify previous owner
            if (previousOwner != null)
            {
                previousOwner.ReleasePossession();
            }

            // Publish event
            EventBus.Instance.Publish(new BallPossessionChangedEvent(previousOwner, null));

            Debug.Log($"Ball released by {previousOwner?.PlayerName ?? "Unknown"} - {(isShot ? "SHOT" : "PASS")}");

            // After a short time, transition back to FREE
            Invoke(nameof(ReturnToFreeState), 0.5f);
        }

        private void ReturnToFreeState()
        {
            if (_currentState == BallState.PASSING || _currentState == BallState.SHOOTING)
            {
                TransitionToState(BallState.FREE);
            }
        }

        #endregion

        #region Collision Handling

        private void OnCollisionEnter(Collision collision)
        {
            // Handle bouncing
            if (_currentState == BallState.PASSING || _currentState == BallState.SHOOTING)
            {
                TransitionToState(BallState.BOUNCING);
                Invoke(nameof(ReturnToFreeState), 0.3f);
            }
        }

        #endregion

        #region Public API

        public void ResetBall(Vector3 position)
        {
            _currentOwner = null;
            transform.position = position;
            TransitionToState(BallState.FREE);

            if (_rigidbody != null)
            {
                _rigidbody.velocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }

            Debug.Log("Ball reset to starting position");
        }

        #endregion

        #region Debug

        private void OnDrawGizmosSelected()
        {
            // Draw possession radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _possessionRadius);

            // Draw connection to owner
            if (_currentOwner != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, _currentOwner.transform.position);
            }
        }

        #endregion
    }
}
