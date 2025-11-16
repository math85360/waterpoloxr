using UnityEngine;
using WaterPolo.Core;
using WaterPolo.Tactics;
using WaterPolo.Ball;

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
        [SerializeField] private float _pursuitDistance = 15f;   // Max distance to pursue ball

        [Header("AI State")]
        [SerializeField] private float _nextDecisionTime = 0f;

        // References
        private GameObject _ball;
        private BallController _ballController;
        private Transform _opponentGoal;
        private Transform _ownGoal; // Cached at Start
        private MatchState _matchState;
        private FormationManager _formationManager;

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
            else
            {
                _ballController = _ball.GetComponent<BallController>();
            }
        }

        protected override void Start()
        {
            base.Start();

            // Determine goals at start (when players are at correct initial positions)
            DetermineGoals();

            // Find match state
            _matchState = FindObjectOfType<MatchState>();

            // Find formation manager
            _formationManager = FindObjectOfType<FormationManager>();
        }

        /// <summary>
        /// Determine own goal and opponent goal based on TeamName.
        /// Called once at Start to cache the correct goals.
        /// </summary>
        private void DetermineGoals()
        {
            GameObject[] goals = GameObject.FindGameObjectsWithTag("Goal");

            if (goals.Length < 2)
            {
                Debug.LogWarning($"{_playerName}: Need at least 2 goals in scene!");
                return;
            }

            // Find goals based on GoalDetector._goalTeam matching player TeamName
            foreach (GameObject goalObj in goals)
            {
                GoalDetector detector = goalObj.GetComponentInChildren<GoalDetector>();
                if (detector == null)
                {
                    // Try on parent
                    detector = goalObj.GetComponentInParent<GoalDetector>();
                }

                if (detector != null)
                {
                    // Use reflection to get _goalTeam (it's private SerializeField)
                    var field = detector.GetType().GetField("_goalTeam",
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance);

                    if (field != null)
                    {
                        string goalTeam = (string)field.GetValue(detector);

                        if (goalTeam == _teamName)
                        {
                            // This is my own goal (I defend it)
                            _ownGoal = goalObj.transform;
                        }
                        else
                        {
                            // This is opponent goal (I attack it)
                            _opponentGoal = goalObj.transform;
                        }
                    }
                }
            }

            // Fallback: If reflection failed, use distance-based detection
            if (_ownGoal == null || _opponentGoal == null)
            {
                Debug.LogWarning($"{_playerName}: Could not find goals by TeamName, using distance fallback");

                float closestDistance = float.MaxValue;
                float furthestDistance = 0f;

                foreach (GameObject goal in goals)
                {
                    float distance = Vector3.Distance(transform.position, goal.transform.position);

                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        _ownGoal = goal.transform;
                    }

                    if (distance > furthestDistance)
                    {
                        furthestDistance = distance;
                        _opponentGoal = goal.transform;
                    }
                }
            }

            if (_ownGoal != null && _opponentGoal != null)
            {
                Debug.Log($"{_playerName} ({_teamName}): Own goal at {_ownGoal.position}, Opponent goal at {_opponentGoal.position}");
            }
            else
            {
                Debug.LogError($"{_playerName}: Failed to determine goals!");
            }
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
            // Check if should pursue ball or maintain formation
            bool shouldPursueBall = ShouldPursueBall();

            if (shouldPursueBall && _ball != null)
            {
                // I'm the closest to the ball in my team, pursue it
                SetTargetPosition(_ball.transform.position);
                SetAction(PlayerAction.Swimming);
            }
            else
            {
                // Maintain formation position
                Vector3 formationPos = GetFormationPosition();
                SetTargetPosition(formationPos);
                SetAction(PlayerAction.Positioning);
            }
        }

        /// <summary>
        /// Determines if this player should pursue the ball.
        /// Only the closest player per team should pursue.
        /// </summary>
        private bool ShouldPursueBall()
        {
            if (_ball == null) return false;

            // Check if ball is possessed
            if (_ballController != null && _ballController.CurrentState == BallState.POSSESSED)
            {
                return false; // Ball is already possessed, maintain formation
            }

            float myDistance = Vector3.Distance(transform.position, _ball.transform.position);

            // Don't pursue if too far
            if (myDistance > _pursuitDistance)
            {
                return false;
            }

            // GOALKEEPER CONSTRAINT: Never go more than 3m from goal position
            if (_role == PlayerRole.Goalkeeper)
            {
                Vector3 goalPosition = GetFormationPosition(); // Own goal
                float distanceFromGoal = Vector3.Distance(_ball.transform.position, goalPosition);

                // Only pursue if ball is within 3m of goal
                if (distanceFromGoal > 3f)
                {
                    return false;
                }
            }

            // Check if I'm the closest teammate to the ball
            AIPlayer[] allPlayers = FindObjectsOfType<AIPlayer>();
            foreach (AIPlayer player in allPlayers)
            {
                // Skip self
                if (player == this) continue;

                // Only check teammates
                if (player.TeamName != this.TeamName) continue;

                // Skip goalkeeper (they should stay in goal)
                if (player.Role == PlayerRole.Goalkeeper) continue;

                // Check if another teammate is closer
                float theirDistance = Vector3.Distance(player.transform.position, _ball.transform.position);
                if (theirDistance < myDistance - 1f) // 1m tolerance to avoid flickering
                {
                    return false; // Someone else is closer
                }
            }

            // I'm the closest, I should pursue
            return true;
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
            // Simple formation position based on role
            // All positions are in own half of the field (defensive formation)
            // In Phase 2+, FormationManager will set _targetPosition directly and handle attack/defense

            if (_ownGoal == null || _opponentGoal == null)
                return transform.position;

            Vector3 ownGoalPosition = _ownGoal.position;
            Vector3 forwardDirection = (_opponentGoal.position - ownGoalPosition).normalized;
            Vector3 rightDirection = Vector3.Cross(forwardDirection, Vector3.up).normalized;

            Vector3 basePosition = ownGoalPosition;

            // ALL positions relative to OWN goal (defensive formation)
            // This keeps all players in their own half of the field
            switch (_role)
            {
                case PlayerRole.Goalkeeper:
                    // Stay at own goal
                    basePosition = ownGoalPosition;
                    break;

                case PlayerRole.CenterForward:
                    // Most advanced position, but still in own half (10m from own goal)
                    basePosition = ownGoalPosition + forwardDirection * 5f;
                    break;

                case PlayerRole.LeftWing:
                    // 8m forward, 4m to the left
                    basePosition = ownGoalPosition + forwardDirection * 2f;
                    basePosition += rightDirection * -4f; // Left
                    break;

                case PlayerRole.RightWing:
                    // 8m forward, 4m to the right
                    basePosition = ownGoalPosition + forwardDirection * 2f;
                    basePosition += rightDirection * 4f; // Right
                    break;

                case PlayerRole.LeftDriver:
                    // Mid-field left (6m from own goal)
                    basePosition = ownGoalPosition + forwardDirection * 5f;
                    basePosition += rightDirection * -3f;
                    break;

                case PlayerRole.RightDriver:
                    // Mid-field right (6m from own goal)
                    basePosition = ownGoalPosition + forwardDirection * 5f;
                    basePosition += rightDirection * 3f;
                    break;

                case PlayerRole.CenterBack:
                    // Defensive position (3m from own goal)
                    basePosition = ownGoalPosition + forwardDirection * 2.5f;
                    break;
            }

            // Keep Y at water level
            basePosition.y = 0f;

            return basePosition;
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
