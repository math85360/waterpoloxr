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

        [Header("Pool Boundaries")]
        [SerializeField] private float _poolMinX = -12.5f;
        [SerializeField] private float _poolMaxX = 12.5f;
        [SerializeField] private float _poolMinZ = -8f;
        [SerializeField] private float _poolMaxZ = 8f;
        [SerializeField] private float _respawnOffset = 0.5f; // 50cm inside pool

        private Rigidbody _rigidbody;
        private BallBuoyancy _buoyancy; // Existing buoyancy script
        private Transform[] _goals; // Cached goals for out-of-bounds detection

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

            // Cache goals for out-of-bounds detection
            GameObject[] goalObjects = GameObject.FindGameObjectsWithTag("Goal");
            _goals = new Transform[goalObjects.Length];
            for (int i = 0; i < goalObjects.Length; i++)
            {
                _goals[i] = goalObjects[i].transform;
            }
        }

        private void Update()
        {
            // Check for out-of-bounds
            CheckOutOfBounds();

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

        /// <summary>
        /// Check if ball is out of pool bounds and handle accordingly.
        /// </summary>
        private void CheckOutOfBounds()
        {
            Vector3 pos = transform.position;

            // Check if ball is outside pool boundaries
            bool outOfBounds = pos.x < _poolMinX || pos.x > _poolMaxX ||
                               pos.z < _poolMinZ || pos.z > _poolMaxZ;

            if (!outOfBounds) return;

            Debug.Log($"Ball out of bounds at {pos}");

            // If ball is possessed and somehow outside, force turnover first
            if (_currentState == BallState.POSSESSED)
            {
                Debug.LogWarning("Ball possessed but outside pool - forcing turnover!");
                ForceTurnover();
            }

            // Cancel any pending state transitions
            CancelInvoke(nameof(ReturnToFreeState));

            // Determine if ball went behind a goal
            Transform closestGoal = null;
            float closestGoalDistance = float.MaxValue;

            foreach (Transform goal in _goals)
            {
                if (goal == null) continue;
                float distance = Vector3.Distance(pos, goal.position);
                if (distance < closestGoalDistance)
                {
                    closestGoalDistance = distance;
                    closestGoal = goal;
                }
            }

            // Check if ball is behind the closest goal (within 3m and beyond goal line)
            bool behindGoal = false;
            if (closestGoal != null && closestGoalDistance < 5f)
            {
                // Ball is near a goal - check if it's behind it
                // Goals are typically at the Z boundaries
                if ((closestGoal.position.z < 0 && pos.z < _poolMinZ) ||
                    (closestGoal.position.z > 0 && pos.z > _poolMaxZ))
                {
                    behindGoal = true;
                }
            }

            if (behindGoal)
            {
                // Ball went behind goal - give to goalkeeper
                GiveBallToGoalkeeper(closestGoal);
            }
            else
            {
                // Ball went out on the sides - respawn inside pool
                RespawnBallInside(pos);
            }
        }

        /// <summary>
        /// Give ball to the goalkeeper of the specified goal.
        /// </summary>
        private void GiveBallToGoalkeeper(Transform goal)
        {
            // Find the goalkeeper for this goal
            WaterPoloPlayer[] players = FindObjectsOfType<WaterPoloPlayer>();
            WaterPoloPlayer goalkeeper = null;
            float closestDistance = float.MaxValue;

            foreach (WaterPoloPlayer player in players)
            {
                if (player.Role == PlayerRole.Goalkeeper)
                {
                    float distance = Vector3.Distance(player.transform.position, goal.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        goalkeeper = player;
                    }
                }
            }

            if (goalkeeper != null)
            {
                // Reset ball velocity and give to goalkeeper
                if (_rigidbody != null)
                {
                    _rigidbody.linearVelocity = Vector3.zero;
                    _rigidbody.angularVelocity = Vector3.zero;
                }

                // Position ball at goalkeeper
                transform.position = goalkeeper.GetArmPosition(ArmSide.Right);
                TryGivePossession(goalkeeper);

                Debug.Log($"Ball given to goalkeeper {goalkeeper.PlayerName} after going behind goal");
            }
            else
            {
                // No goalkeeper found, just respawn at goal
                Vector3 respawnPos = goal.position + Vector3.forward * _respawnOffset * Mathf.Sign(goal.position.z) * -1f;
                respawnPos.y = 0f;
                ResetBall(respawnPos);
                Debug.Log("Ball respawned at goal (no goalkeeper found)");
            }
        }

        /// <summary>
        /// Respawn ball 50cm inside the pool boundary.
        /// </summary>
        private void RespawnBallInside(Vector3 outPosition)
        {
            Vector3 respawnPos = outPosition;

            // Clamp to pool boundaries with offset
            if (outPosition.x < _poolMinX)
                respawnPos.x = _poolMinX + _respawnOffset;
            else if (outPosition.x > _poolMaxX)
                respawnPos.x = _poolMaxX - _respawnOffset;

            if (outPosition.z < _poolMinZ)
                respawnPos.z = _poolMinZ + _respawnOffset;
            else if (outPosition.z > _poolMaxZ)
                respawnPos.z = _poolMaxZ - _respawnOffset;

            // Reset at water level
            respawnPos.y = 0f;

            ResetBall(respawnPos);
            Debug.Log($"Ball respawned inside pool at {respawnPos}");
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
                // Set velocities to zero BEFORE making kinematic
                // (can't set velocity on kinematic body)
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
                _rigidbody.isKinematic = true;
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
                _rigidbody.linearVelocity = velocity * forceMultiplier;
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
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }

            Debug.Log("Ball reset to starting position");
        }

        /// <summary>
        /// Force a turnover - release ball without giving it to anyone.
        /// Used for shot clock violations, fouls, etc.
        /// </summary>
        public void ForceTurnover()
        {
            if (_currentState != BallState.POSSESSED)
            {
                return; // Ball not possessed, nothing to do
            }

            WaterPoloPlayer previousOwner = _currentOwner;

            // Release from current owner
            if (previousOwner != null)
            {
                previousOwner.ReleasePossession();
            }

            _currentOwner = null;
            TransitionToState(BallState.FREE);

            // Publish event
            EventBus.Instance.Publish(new BallPossessionChangedEvent(previousOwner, null));

            Debug.Log($"Turnover! Ball released from {previousOwner?.PlayerName ?? "Unknown"}");
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
