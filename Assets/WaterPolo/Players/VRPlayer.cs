using UnityEngine;
using UnityEngine.XR;
using WaterPolo.Core;
using WaterPolo.Ball;

namespace WaterPolo.Players
{
    /// <summary>
    /// VR-controlled water polo player.
    /// Integrates with Oculus/Meta Quest input and tracking.
    /// Phase 1: Basic VR control with manual input.
    /// Future phases will add gesture recognition, stamina, and advanced mechanics.
    /// </summary>
    public class VRPlayer : WaterPoloPlayer
    {
        [Header("VR Configuration")]
        [SerializeField] private Transform _headTransform; // VR camera/head
        [SerializeField] private bool _useVRMovement = true;

        [Header("VR Hand Tracking")]
        [SerializeField] private Transform _vrLeftHand;
        [SerializeField] private Transform _vrRightHand;

        [Header("Input")]
        [SerializeField] private OVRInput.Controller _primaryController = OVRInput.Controller.RTouch;
        [SerializeField] private OVRInput.Controller _secondaryController = OVRInput.Controller.LTouch;

        [Header("Movement")]
        [SerializeField] private float _vrSwimSpeed = 2.0f;
        [SerializeField] private bool _useThumbstickMovement = true;

        [Header("Ball Interaction")]
        [SerializeField] private BallGrabAndThrow _ballGrabSystem; // Reference to existing system

        private BallController _ballController;
        private Vector3 _movementInput;

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();

            // Find VR components if not assigned
            if (_headTransform == null)
            {
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    _headTransform = mainCam.transform;
                }
            }

            // Find ball controller
            GameObject ball = GameObject.FindGameObjectWithTag("Ball");
            if (ball != null)
            {
                _ballController = ball.GetComponent<BallController>();
            }

            // Find ball grab system if not assigned
            if (_ballGrabSystem == null)
            {
                _ballGrabSystem = GetComponentInChildren<BallGrabAndThrow>();
            }
        }

        protected override void Start()
        {
            base.Start();

            // Override left/right hand transforms with VR hands
            if (_vrLeftHand != null)
            {
                _leftHandTransform = _vrLeftHand;
            }
            if (_vrRightHand != null)
            {
                _rightHandTransform = _vrRightHand;
            }
        }

        protected override void Update()
        {
            base.Update();

            // VR-specific updates
            if (_useVRMovement)
            {
                UpdateVRMovement();
            }

            // Update position to follow head (body follows head in VR)
            if (_headTransform != null)
            {
                Vector3 headPosition = _headTransform.position;
                headPosition.y = transform.position.y; // Keep same height (water level)
                transform.position = Vector3.Lerp(transform.position, headPosition, Time.deltaTime * 5f);

                // Orient body based on head forward (projected on horizontal plane)
                Vector3 headForward = _headTransform.forward;
                headForward.y = 0;
                if (headForward != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(headForward);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 3f);
                }
            }
        }

        #endregion

        #region Decision Making (VR Input)

        public override void DecideAction()
        {
            // VR player actions are driven by input, not AI
            // Read controller input and update current action

            if (_hasBall)
            {
                // Check for throw/shoot input (handled by BallGrabAndThrow)
                _currentAction = PlayerAction.Idle; // Ball grab system handles this
            }
            else
            {
                // Check movement input
                if (_movementInput.magnitude > 0.1f)
                {
                    _currentAction = PlayerAction.Swimming;
                }
                else
                {
                    _currentAction = PlayerAction.Idle;
                }
            }
        }

        #endregion

        #region Action Execution

        protected override void ExecuteAction()
        {
            // VR player execution is handled by input and physics
            // Most actions are passive or handled by other systems
            switch (_currentAction)
            {
                case PlayerAction.Swimming:
                    // Movement already handled in UpdateVRMovement
                    break;

                case PlayerAction.Idle:
                    // Do nothing
                    break;
            }
        }

        #endregion

        #region VR Movement

        private void UpdateVRMovement()
        {
            if (!_useThumbstickMovement) return;

            // Get thumbstick input from primary controller
            Vector2 primaryThumbstick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);

            // Convert to world space movement
            Vector3 moveDirection = Vector3.zero;

            if (_headTransform != null)
            {
                // Move relative to head forward direction (projected on horizontal plane)
                Vector3 forward = _headTransform.forward;
                forward.y = 0;
                forward.Normalize();

                Vector3 right = _headTransform.right;
                right.y = 0;
                right.Normalize();

                moveDirection = (forward * primaryThumbstick.y + right * primaryThumbstick.x);
            }
            else
            {
                // Fallback: world space movement
                moveDirection = new Vector3(primaryThumbstick.x, 0, primaryThumbstick.y);
            }

            _movementInput = moveDirection;

            // Apply movement
            if (moveDirection.magnitude > 0.1f)
            {
                transform.position += moveDirection * _vrSwimSpeed * Time.deltaTime;
            }
        }

        #endregion

        #region Arm/Hand Interface Implementation

        public override Vector3 GetArmPosition(ArmSide side)
        {
            Transform handTransform = side == ArmSide.Left ? _leftHandTransform : _rightHandTransform;

            if (handTransform != null)
            {
                return handTransform.position;
            }
            else
            {
                // Fallback to estimated position
                Vector3 offset = side == ArmSide.Left ? Vector3.left * 0.3f : Vector3.right * 0.3f;
                return transform.position + transform.TransformDirection(offset) + Vector3.up * 0.5f;
            }
        }

        public override Quaternion GetHandRotation(ArmSide side)
        {
            Transform handTransform = side == ArmSide.Left ? _leftHandTransform : _rightHandTransform;

            if (handTransform != null)
            {
                return handTransform.rotation;
            }
            else
            {
                return transform.rotation;
            }
        }

        #endregion

        #region Ball Handling Override

        public override void TakePossession(GameObject ball)
        {
            base.TakePossession(ball);

            // VR possession is handled by BallGrabAndThrow system
            // This just updates the internal state
        }

        public override void ReleasePossession()
        {
            base.ReleasePossession();

            // VR release is handled by BallGrabAndThrow system
            // This just updates the internal state
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get current movement input (for UI/debug).
        /// </summary>
        public Vector3 GetMovementInput()
        {
            return _movementInput;
        }

        /// <summary>
        /// Check if player is pressing a specific button.
        /// </summary>
        public bool IsButtonPressed(OVRInput.Button button)
        {
            return OVRInput.Get(button);
        }

        #endregion

        #region Debug

        private void OnDrawGizmos()
        {
            // Draw movement input direction
            if (_movementInput.magnitude > 0.1f)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(transform.position, _movementInput * 2f);
            }

            // Draw hand positions
            if (_leftHandTransform != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(_leftHandTransform.position, 0.05f);
            }

            if (_rightHandTransform != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(_rightHandTransform.position, 0.05f);
            }
        }

        #endregion
    }
}
