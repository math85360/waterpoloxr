using UnityEngine;

namespace WaterPolo.Referee
{
    /// <summary>
    /// ScriptableObject defining referee behavior characteristics.
    /// Allows different referee personalities (strict, permissive, etc.).
    /// </summary>
    [CreateAssetMenu(fileName = "RefereeProfile", menuName = "WaterPolo/RefereeProfile", order = 2)]
    public class RefereeProfile : ScriptableObject
    {
        [Header("Tolerance")]
        [Tooltip("0 = Very permissive, 1 = Very strict")]
        [Range(0f, 1f)] public float strictness = 0.5f;

        [Tooltip("Tendency to apply advantage rule (wait for offensive action)")]
        [Range(0f, 1f)] public float advantageOrientation = 0.7f;

        [Tooltip("Consistency - same foul = same decision")]
        [Range(0f, 1f)] public float consistencyLevel = 0.9f;

        [Header("Detection")]
        [Tooltip("Vision accuracy - how well referee sees fouls")]
        [Range(0f, 1f)] public float visionAccuracy = 0.9f;

        [Tooltip("Vigilance on 2-meter line violations")]
        [Range(0f, 1f)] public float twoMeterLineVigilance = 0.8f;

        [Tooltip("Threshold for calling penalty instead of ordinary foul")]
        [Range(0f, 1f)] public float penaltyThreshold = 0.6f;

        [Tooltip("Error rate - % of wrong calls")]
        [Range(0f, 0.2f)] public float errorRate = 0.05f;

        [Header("Specific Rules")]
        [Tooltip("Tendency to call exclusions vs ordinary fouls")]
        [Range(0f, 1f)] public float exclusionTendency = 0.5f;

        [Tooltip("Strictness on hand-check after foul")]
        [Range(0f, 1f)] public float handCheckStrictness = 0.7f;

        [Header("Physical")]
        [Tooltip("Movement speed along pool edge (m/s)")]
        public float movementSpeed = 2.5f;

        [Tooltip("How far ahead referee anticipates play")]
        public float anticipationDistance = 1.5f;

        [Header("Advantage Rule")]
        [Tooltip("Maximum time to wait for advantage (seconds)")]
        public float maxAdvantageTime = 1.5f;

        [Tooltip("Minimum distance to goal to consider advantage")]
        public float advantageGoalDistance = 10f;

        /// <summary>
        /// Determine if a foul should be called based on severity and context.
        /// </summary>
        public bool ShouldCallFoul(float foulSeverity, bool hasAdvantageOpportunity)
        {
            // Apply strictness threshold
            float threshold = Mathf.Lerp(0.7f, 0.3f, strictness);

            if (foulSeverity < threshold)
                return false; // Too minor

            // If advantage opportunity exists and ref is advantage-oriented
            if (hasAdvantageOpportunity && Random.value < advantageOrientation)
                return false; // Let play continue

            // Apply error rate
            if (Random.value < errorRate)
                return Random.value > 0.5f; // Random wrong decision

            return true;
        }

        /// <summary>
        /// Determine if foul warrants exclusion vs ordinary foul.
        /// </summary>
        public bool ShouldCallExclusion(float foulSeverity, bool isBrutality)
        {
            if (isBrutality)
                return true; // Always exclude for brutality

            float threshold = Mathf.Lerp(0.8f, 0.5f, exclusionTendency);
            return foulSeverity >= threshold;
        }

        /// <summary>
        /// Determine if foul should result in penalty.
        /// </summary>
        public bool ShouldCallPenalty(float foulSeverity, bool preventedGoalOpportunity)
        {
            if (!preventedGoalOpportunity)
                return false;

            return foulSeverity >= penaltyThreshold;
        }
    }
}
