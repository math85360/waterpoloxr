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

        [Header("State")]
        [SerializeField] protected Vector3 _targetPosition;
        [SerializeField] protected PlayerAction _currentAction = PlayerAction.Idle;
        [SerializeField] protected bool _hasBall = false;

        [Header("Movement")]
        [SerializeField] protected float _swimSpeed = 1.5f;
        [SerializeField] protected float _rotationSpeed = 180f;

        [Header("References")]
        [SerializeField] protected Transform _leftHandTransform;
        [SerializeField] protected Transform _rightHandTransform;

        #region Properties

        public string PlayerName => _playerName;
        public PlayerRole Role => _role;
        public string TeamName => _teamName;
        public Vector3 TargetPosition => _targetPosition;
        public PlayerAction CurrentAction => _currentAction;
        public bool HasBall => _hasBall;
        public float SwimSpeed => _swimSpeed;

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
            // Execute current action
            ExecuteAction();
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
        /// </summary>
        public virtual void SetTargetPosition(Vector3 position)
        {
            _targetPosition = position;
        }

        /// <summary>
        /// Basic swimming movement towards target position.
        /// Can be overridden for custom movement behavior.
        /// </summary>
        protected virtual void SwimTowardsTarget()
        {
            if (_targetPosition == Vector3.zero) return;

            Vector3 direction = (_targetPosition - transform.position).normalized;

            // Move towards target
            Vector3 movement = direction * _swimSpeed * Time.deltaTime;
            transform.position += movement;

            // Rotate towards target
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    _rotationSpeed * Time.deltaTime
                );
            }

            // Check if reached target
            if (Vector3.Distance(transform.position, _targetPosition) < 0.5f)
            {
                _currentAction = PlayerAction.Idle;
            }
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
