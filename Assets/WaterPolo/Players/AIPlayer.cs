using UnityEngine;
using WaterPolo.Core;

namespace WaterPolo.Players
{
    /// <summary>
    /// AI-controlled water polo player.
    /// Phase 1: Simple movement and basic decision-making.
    /// Future phases will add tactical AI, formations, and advanced behavior.
    /// </summary>
    public class AIPlayer : WaterPoloPlayer
    {
        [Header("AI Configuration")]
        [SerializeField] private float _decisionInterval = 0.5f; // How often to make decisions
        [SerializeField] private float _reactionTime = 0.2f;     // Delay before acting

        [Header("AI State")]
        [SerializeField] private float _nextDecisionTime = 0f;

        // References
        private GameObject _ball;
        private Transform _opponentGoal;
        private MatchState _matchState;

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();

            // Find the ball
            _ball = GameObject.FindGameObjectWithTag("Ball");
            if (_ball == null)
            {
                Debug.LogWarning($"{_playerName}: No ball found with tag 'Ball'");
            }
        }

        protected override void Start()
        {
            base.Start();

            // Find opponent goal
            FindOpponentGoal();

            // Find match state
            _matchState = FindObjectOfType<MatchState>();
        }

        protected override void Update()
        {
            base.Update();

            // Make decisions at intervals
            if (Time.time >= _nextDecisionTime)
            {
                DecideAction();
                _nextDecisionTime = Time.time + _decisionInterval;
            }
        }

        #endregion

        #region Decision Making (Phase 1 - Simple)

        public override void DecideAction()
        {
            // Don't decide if match isn't playing
            if (_matchState != null && !_matchState.CanMove)
            {
                SetAction(PlayerAction.Idle);
                return;
            }

            // Simple decision tree for Phase 1
            if (_hasBall)
            {
                DecideWithBall();
            }
            else
            {
                DecideWithoutBall();
            }
        }

        private void DecideWithBall()
        {
            // Phase 1: Simple logic - if close to goal, shoot, otherwise move forward
            if (_opponentGoal != null)
            {
                float distanceToGoal = Vector3.Distance(transform.position, _opponentGoal.position);

                if (distanceToGoal < 8f && _matchState != null && _matchState.CanShoot)
                {
                    // Close enough to shoot
                    SetAction(PlayerAction.Shooting);
                }
                else
                {
                    // Move towards goal
                    SetTargetPosition(GetPositionTowardsGoal());
                    SetAction(PlayerAction.Swimming);
                }
            }
        }

        private void DecideWithoutBall()
        {
            // Phase 1: Simple logic - move towards ball if it's free, or towards formation position
            if (_ball != null)
            {
                // Check if ball is free (simplified for Phase 1)
                // In Phase 2+, we'll check BallController state
                float distanceToBall = Vector3.Distance(transform.position, _ball.transform.position);

                if (distanceToBall < 10f)
                {
                    // Move towards ball
                    SetTargetPosition(_ball.transform.position);
                    SetAction(PlayerAction.Swimming);
                }
                else
                {
                    // Move to formation position (simplified - just a position relative to goal)
                    SetTargetPosition(GetFormationPosition());
                    SetAction(PlayerAction.Positioning);
                }
            }
        }

        #endregion

        #region Action Execution

        protected override void ExecuteAction()
        {
            switch (_currentAction)
            {
                case PlayerAction.Swimming:
                case PlayerAction.Positioning:
                    SwimTowardsTarget();
                    break;

                case PlayerAction.Shooting:
                    ExecuteShoot();
                    break;

                case PlayerAction.Idle:
                    // Do nothing
                    break;
            }
        }

        private void ExecuteShoot()
        {
            if (!_hasBall || _opponentGoal == null) return;

            // Phase 1: Simple shoot - just release ball towards goal
            // In Phase 2+, we'll add velocity, aim variance, etc.

            Debug.Log($"{_playerName} shoots towards goal!");

            // Release ball
            ReleasePossession();

            // Apply force to ball (simplified)
            if (_ball != null)
            {
                Rigidbody ballRb = _ball.GetComponent<Rigidbody>();
                if (ballRb != null)
                {
                    Vector3 shootDirection = (_opponentGoal.position - transform.position).normalized;
                    ballRb.AddForce(shootDirection * 15f, ForceMode.Impulse);
                }
            }

            // Return to positioning
            SetAction(PlayerAction.Positioning);
        }

        #endregion

        #region Arm/Hand Interface Implementation

        public override Vector3 GetArmPosition(ArmSide side)
        {
            // Phase 1: Use hand transforms if available, otherwise estimate
            Transform handTransform = side == ArmSide.Left ? _leftHandTransform : _rightHandTransform;

            if (handTransform != null)
            {
                return handTransform.position;
            }
            else
            {
                // Estimate hand position relative to body
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

        #region Position Calculation

        private Vector3 GetPositionTowardsGoal()
        {
            if (_opponentGoal == null) return transform.position;

            // Move 2 meters towards goal
            Vector3 directionToGoal = (_opponentGoal.position - transform.position).normalized;
            return transform.position + directionToGoal * 2f;
        }

        private Vector3 GetFormationPosition()
        {
            // Phase 1: Simple formation position based on role
            // In Phase 2+, this will use WaterPoloFormation ScriptableObject

            if (_opponentGoal == null) return transform.position;

            Vector3 goalPosition = _opponentGoal.position;
            Vector3 basePosition = goalPosition;

            // Position relative to goal based on role
            switch (_role)
            {
                case PlayerRole.CenterForward:
                    basePosition += Vector3.back * 5f; // 5m in front of goal
                    break;

                case PlayerRole.LeftWing:
                    basePosition += Vector3.back * 6f + Vector3.left * 3f;
                    break;

                case PlayerRole.RightWing:
                    basePosition += Vector3.back * 6f + Vector3.right * 3f;
                    break;

                case PlayerRole.LeftDriver:
                    basePosition += Vector3.back * 8f + Vector3.left * 2f;
                    break;

                case PlayerRole.RightDriver:
                    basePosition += Vector3.back * 8f + Vector3.right * 2f;
                    break;

                case PlayerRole.CenterBack:
                    basePosition += Vector3.back * 10f;
                    break;

                case PlayerRole.Goalkeeper:
                    basePosition = goalPosition;
                    break;
            }

            return basePosition;
        }

        private void FindOpponentGoal()
        {
            // Find goals in scene
            GameObject[] goals = GameObject.FindGameObjectsWithTag("Goal");

            foreach (GameObject goal in goals)
            {
                // Simple check: opponent goal is the one furthest away
                // In Phase 2+, goals will have team association
                if (_opponentGoal == null ||
                    Vector3.Distance(transform.position, goal.transform.position) >
                    Vector3.Distance(transform.position, _opponentGoal.position))
                {
                    _opponentGoal = goal.transform;
                }
            }

            if (_opponentGoal == null)
            {
                Debug.LogWarning($"{_playerName}: No goal found with tag 'Goal'");
            }
        }

        #endregion

        #region Debug

        private void OnDrawGizmosSelected()
        {
            // Draw target position
            if (_targetPosition != Vector3.zero)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(_targetPosition, 0.3f);
                Gizmos.DrawLine(transform.position, _targetPosition);
            }

            // Draw formation position
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(GetFormationPosition(), 0.2f);
        }

        #endregion
    }
}
