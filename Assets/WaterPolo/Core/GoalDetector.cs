using UnityEngine;

namespace WaterPolo.Core
{
    /// <summary>
    /// Detects when the ball crosses the goal line.
    /// Should be attached to a trigger collider positioned at the goal line.
    /// Acts as a GoalJudge as described in CLAUDE.md architecture.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class GoalDetector : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private string _goalTeam = "Home"; // Which team this goal belongs to
        [SerializeField] private bool _requireFullBallCrossing = true; // Ball must fully cross line

        [Header("Debug")]
        [SerializeField] private bool _visualizeDetection = true;
        [SerializeField] private Color _detectionColor = Color.green;

        private ScoreTable _scoreTable;
        private BoxCollider _triggerCollider;
        private GameObject _lastBallDetected;

        #region Unity Lifecycle

        private void Awake()
        {
            // Verify trigger collider
            _triggerCollider = GetComponent<BoxCollider>();
            if (!_triggerCollider.isTrigger)
            {
                Debug.LogWarning($"GoalDetector on {gameObject.name}: BoxCollider should be a trigger!");
                _triggerCollider.isTrigger = true;
            }

            // Find score table
            _scoreTable = FindObjectOfType<ScoreTable>();
            if (_scoreTable == null)
            {
                Debug.LogError("GoalDetector: No ScoreTable found in scene!");
            }
        }

        #endregion

        #region Collision Detection

        private void OnTriggerEnter(Collider other)
        {
            // Check if it's the ball
            if (other.CompareTag("Ball"))
            {
                DetectGoal(other.gameObject);
            }
        }

        private void DetectGoal(GameObject ball)
        {
            // Prevent duplicate detection
            if (ball == _lastBallDetected)
                return;

            _lastBallDetected = ball;

            // Determine scoring team (opponent of goal owner)
            string scoringTeam = GetOpponentTeam(_goalTeam);

            // Try to find who shot the ball (simplified for Phase 1)
            // In Phase 2+, BallController will track last player who touched it
            MonoBehaviour scorer = FindBallOwner(ball);

            // Register goal with score table
            if (_scoreTable != null)
            {
                bool goalValid = _scoreTable.RegisterGoal(scoringTeam, scorer);

                if (goalValid)
                {
                    OnGoalScored(scoringTeam, scorer);
                }
            }
            else
            {
                Debug.LogWarning("Cannot register goal - no ScoreTable found!");
            }
        }

        #endregion

        #region Helper Methods

        private string GetOpponentTeam(string team)
        {
            // Simple logic for Phase 1: Home vs Away
            if (_scoreTable != null)
            {
                return team == _scoreTable.HomeTeamName ? _scoreTable.AwayTeamName : _scoreTable.HomeTeamName;
            }
            return team == "Home" ? "Away" : "Home";
        }

        private MonoBehaviour FindBallOwner(GameObject ball)
        {
            // Phase 1: Find closest player as scorer (simplified)
            // In Phase 2+, BallController will track possession history

            WaterPolo.Players.WaterPoloPlayer[] allPlayers = FindObjectsOfType<WaterPolo.Players.WaterPoloPlayer>();
            MonoBehaviour closestPlayer = null;
            float closestDistance = Mathf.Infinity;

            foreach (var player in allPlayers)
            {
                float distance = Vector3.Distance(player.transform.position, ball.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPlayer = player;
                }
            }

            return closestPlayer;
        }

        private void OnGoalScored(string scoringTeam, MonoBehaviour scorer)
        {
            Debug.Log($"GOAL! {scoringTeam} scores! Scorer: {scorer?.name ?? "Unknown"}");

            // Visual feedback
            if (_visualizeDetection)
            {
                StartCoroutine(GoalFlashCoroutine());
            }

            // Reset after short delay
            Invoke(nameof(ResetDetection), 2f);
        }

        private void ResetDetection()
        {
            _lastBallDetected = null;
        }

        #endregion

        #region Visual Feedback

        private System.Collections.IEnumerator GoalFlashCoroutine()
        {
            float duration = 1f;
            float elapsed = 0f;

            MeshRenderer meshRenderer = GetComponentInParent<MeshRenderer>();
            if (meshRenderer != null)
            {
                Color originalColor = Color.white;
                Material[] materials = meshRenderer.materials;

                while (elapsed < duration)
                {
                    float alpha = Mathf.PingPong(elapsed * 4f, 1f);
                    foreach (Material mat in materials)
                    {
                        Color flashColor = Color.Lerp(originalColor, _detectionColor, alpha);
                        // This is simplified - actual implementation may vary
                    }
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }
        }

        #endregion

        #region Debug Visualization

        private void OnDrawGizmos()
        {
            if (!_visualizeDetection) return;

            BoxCollider col = GetComponent<BoxCollider>();
            if (col == null) return;

            Gizmos.color = _detectionColor;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(col.center, col.size);
        }

        #endregion
    }
}
