using UnityEngine;
using WaterPolo.Core;

namespace WaterPolo.Players
{
    /// <summary>
    /// Player roles in water polo.
    /// Determines tactical positioning and responsibilities.
    /// </summary>
    public enum PlayerRole
    {
        Goalkeeper,      // Gardien de but
        CenterForward,   // Pointe (pivot)
        LeftWing,        // Ailier gauche
        RightWing,       // Ailier droit
        LeftDriver,      // Demi gauche
        RightDriver,     // Demi droit
        CenterBack       // Défense centrale / défense-pointe
    }

    /// <summary>
    /// Current action being performed by a player.
    /// </summary>
    public enum PlayerAction
    {
        Idle,
        Swimming,
        Receiving,
        Passing,
        Shooting,
        Defending,
        Positioning
    }

    /// <summary>
    /// Abstract base class for all water polo players.
    /// Provides common interface for AI, VR, and Observed players.
    /// </summary>
    public abstract class WaterPoloPlayer : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] protected string _playerName = "Player";
        [SerializeField] protected PlayerRole _role = PlayerRole.LeftDriver;
        [SerializeField] protected string _teamName = "Home";

        [Header("Attributes")]
        [SerializeField] protected PlayerAttributes _attributes;

        [Header("State")]
        [SerializeField] protected Vector3 _targetPosition;
        [SerializeField] protected PlayerAction _currentAction = PlayerAction.Idle;
        [SerializeField] protected bool _hasBall = false;
        [SerializeField] protected float _currentFatigue = 0f; // 0-1, 1=exhausted

        [Header("Movement")]
        [SerializeField] protected float _swimSpeed = 1.5f;
        [SerializeField] protected float _rotationSpeed = 180f;

        [Header("References")]
        [SerializeField] protected Transform _leftHandTransform;
        [SerializeField] protected Transform _rightHandTransform;

        [Header("Physics")]
        [SerializeField] protected Rigidbody _rigidbody;

        [Header("Pool Boundaries")]
        [SerializeField] protected float _poolMinX = -12.5f;
        [SerializeField] protected float _poolMaxX = 12.5f;
        [SerializeField] protected float _poolMinZ = -8f;
        [SerializeField] protected float _poolMaxZ = 8f;
        [SerializeField] protected float _boundaryOffset = 0.3f; // Stay 30cm from edge

        #region Properties

        public string PlayerName => _playerName;
        public PlayerRole Role => _role;
        public string TeamName => _teamName;
        public PlayerAttributes Attributes => _attributes;
        public Vector3 TargetPosition => _targetPosition;
        public PlayerAction CurrentAction => _currentAction;
        public bool HasBall => _hasBall;
        public float SwimSpeed => _attributes != null ? _attributes.GetActualSwimSpeed() : _swimSpeed;
        public float CurrentFatigue => _currentFatigue;

        #endregion

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            // Find hand transforms if not assigned
            if (_leftHandTransform == null)
            {
                _leftHandTransform = transform.Find("LeftHand");
            }
            if (_rightHandTransform == null)
            {
                _rightHandTransform = transform.Find("RightHand");
            }

            // Get or add Rigidbody
            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody>();
            }
        }

        protected virtual void Start()
        {
            // Subscribe to events
            EventBus.Instance.Subscribe<BallPossessionChangedEvent>(OnBallPossessionChanged);
        }

        protected virtual void OnDestroy()
        {
            // Unsubscribe from events
            EventBus.Instance.Unsubscribe<BallPossessionChangedEvent>(OnBallPossessionChanged);
        }

        protected virtual void Update()
        {
            // Always enforce pool boundaries (in case player got outside somehow)
            EnforcePoolBoundaries();

            // Execute current action
            ExecuteAction();
        }

        /// <summary>
        /// Ensure player stays within pool boundaries at all times.
        /// Called every frame as a safety check.
        /// </summary>
        protected virtual void EnforcePoolBoundaries()
        {
            Vector3 currentPos = transform.position;
            Vector3 clampedPos = ClampToPoolBounds(currentPos);

            // Only update if actually outside bounds
            if (currentPos.x != clampedPos.x || currentPos.z != clampedPos.z)
            {
                clampedPos.y = currentPos.y; // Preserve Y

                if (_rigidbody != null && !_rigidbody.isKinematic)
                {
                    _rigidbody.MovePosition(clampedPos);
                    // Stop velocity towards the boundary
                    Vector3 vel = _rigidbody.linearVelocity;
                    if ((currentPos.x < _poolMinX && vel.x < 0) || (currentPos.x > _poolMaxX && vel.x > 0))
                        vel.x = 0;
                    if ((currentPos.z < _poolMinZ && vel.z < 0) || (currentPos.z > _poolMaxZ && vel.z > 0))
                        vel.z = 0;
                    _rigidbody.linearVelocity = vel;
                }
                else
                {
                    transform.position = clampedPos;
                }
            }
        }

        #endregion

        #region Abstract Interface

        /// <summary>
        /// Decide what action to take this frame.
        /// AI players use decision-making logic, VR players use input.
        /// </summary>
        public abstract void DecideAction();

        /// <summary>
        /// Execute the current action.
        /// Called every frame while action is active.
        /// </summary>
        protected abstract void ExecuteAction();

        /// <summary>
        /// Get position of specified arm.
        /// Used for ball handling and foul detection.
        /// </summary>
        public abstract Vector3 GetArmPosition(ArmSide side);

        /// <summary>
        /// Get rotation of specified hand.
        /// Used for ball throwing direction.
        /// </summary>
        public abstract Quaternion GetHandRotation(ArmSide side);

        #endregion

        #region Ball Handling

        /// <summary>
        /// Give the ball to this player.
        /// </summary>
        public virtual void TakePossession(GameObject ball)
        {
            _hasBall = true;
            EventBus.Instance.Publish(new BallPossessionChangedEvent(null, this));
            Debug.Log($"{_playerName} took possession of the ball");
        }

        /// <summary>
        /// Release the ball from this player.
        /// </summary>
        public virtual void ReleasePossession()
        {
            _hasBall = false;
            EventBus.Instance.Publish(new BallPossessionChangedEvent(this, null));
            Debug.Log($"{_playerName} released the ball");
        }

        protected virtual void OnBallPossessionChanged(BallPossessionChangedEvent evt)
        {
            // Update local state
            _hasBall = (evt.NewOwner == this);
        }

        #endregion

        #region Movement

        /// <summary>
        /// Set target position for this player to move towards.
        /// Position is automatically clamped to pool boundaries.
        /// </summary>
        public virtual void SetTargetPosition(Vector3 position)
        {
            _targetPosition = ClampToPoolBounds(position);
        }

        /// <summary>
        /// Basic swimming movement towards target position.
        /// Can be overridden for custom movement behavior.
        /// Uses physics-based movement for smooth collision handling.
        /// </summary>
        protected virtual void SwimTowardsTarget()
        {
            if (_targetPosition == Vector3.zero) return;
            if (_rigidbody == null) return;

            Vector3 direction = (_targetPosition - transform.position).normalized;

            // Move towards target using physics
            Vector3 movement = direction * _swimSpeed * Time.fixedDeltaTime;
            Vector3 newPosition = _rigidbody.position + movement;

            // Keep Y position at water level (0)
            newPosition.y = 0f;

            // Clamp position to pool boundaries
            newPosition = ClampToPoolBounds(newPosition);

            _rigidbody.MovePosition(newPosition);

            // Rotate towards target
            if (direction != Vector3.zero)
            {
                Vector3 horizontalDirection = new Vector3(direction.x, 0, direction.z).normalized;
                if (horizontalDirection != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(horizontalDirection);
                    Quaternion newRotation = Quaternion.RotateTowards(
                        _rigidbody.rotation,
                        targetRotation,
                        _rotationSpeed * Time.fixedDeltaTime
                    );
                    _rigidbody.MoveRotation(newRotation);
                }
            }

            // Check if reached target
            if (Vector3.Distance(transform.position, _targetPosition) < 0.5f)
            {
                _currentAction = PlayerAction.Idle;
            }
        }

        /// <summary>
        /// Clamp a position to stay within pool boundaries.
        /// </summary>
        protected Vector3 ClampToPoolBounds(Vector3 position)
        {
            position.x = Mathf.Clamp(position.x, _poolMinX + _boundaryOffset, _poolMaxX - _boundaryOffset);
            position.z = Mathf.Clamp(position.z, _poolMinZ + _boundaryOffset, _poolMaxZ - _boundaryOffset);
            return position;
        }

        #endregion

        #region Action Management

        public virtual void SetAction(PlayerAction action)
        {
            if (_currentAction == action) return;

            PlayerAction previousAction = _currentAction;
            _currentAction = action;

            OnActionChanged(previousAction, action);
        }

        protected virtual void OnActionChanged(PlayerAction previous, PlayerAction current)
        {
            Debug.Log($"{_playerName}: {previous} → {current}");
        }

        #endregion

        #region Utilities

        public bool IsOnTeam(string teamName)
        {
            return _teamName == teamName;
        }

        public float GetDistanceTo(Vector3 position)
        {
            return Vector3.Distance(transform.position, position);
        }

        public float GetDistanceTo(WaterPoloPlayer otherPlayer)
        {
            return Vector3.Distance(transform.position, otherPlayer.transform.position);
        }

        #endregion
    }

    /// <summary>
    /// Enum for arm/hand selection.
    /// </summary>
    public enum ArmSide
    {
        Left,
        Right
    }
}
