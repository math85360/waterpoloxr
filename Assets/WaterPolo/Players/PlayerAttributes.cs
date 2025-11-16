using UnityEngine;

namespace WaterPolo.Players
{
    /// <summary>
    /// ScriptableObject defining a player's attributes and characteristics.
    /// All attributes are normalized 0-1 for consistent scaling.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerAttributes", menuName = "WaterPolo/PlayerAttributes", order = 3)]
    public class PlayerAttributes : ScriptableObject
    {
        [Header("Physical Attributes")]
        [Tooltip("Maximum swimming speed")]
        [Range(0f, 1f)] public float swimSpeed = 0.5f;

        [Tooltip("Acceleration and explosiveness")]
        [Range(0f, 1f)] public float acceleration = 0.5f;

        [Tooltip("Resistance to fatigue over match duration")]
        [Range(0f, 1f)] public float endurance = 0.5f;

        [Tooltip("Physical strength for contact and shooting power")]
        [Range(0f, 1f)] public float strength = 0.5f;

        [Tooltip("Vertical reach for interceptions and blocking")]
        [Range(0f, 1f)] public float verticalReach = 0.5f;

        [Header("Technical Attributes")]
        [Tooltip("Shooting power")]
        [Range(0f, 1f)] public float shotPower = 0.5f;

        [Tooltip("Shooting accuracy")]
        [Range(0f, 1f)] public float shotAccuracy = 0.5f;

        [Tooltip("Passing accuracy")]
        [Range(0f, 1f)] public float passAccuracy = 0.5f;

        [Tooltip("Ball control under pressure")]
        [Range(0f, 1f)] public float ballControl = 0.5f;

        [Tooltip("Ability to catch difficult passes")]
        [Range(0f, 1f)] public float catchAbility = 0.5f;

        [Header("Tactical Attributes")]
        [Tooltip("Reading the game and recognizing defensive schemes")]
        [Range(0f, 1f)] public float gameReading = 0.5f;

        [Tooltip("Tactical positioning awareness")]
        [Range(0f, 1f)] public float positioning = 0.5f;

        [Tooltip("Anticipation of opponent actions")]
        [Range(0f, 1f)] public float anticipation = 0.5f;

        [Tooltip("Speed of decision making")]
        [Range(0f, 1f)] public float decisionSpeed = 0.5f;

        [Header("Mental Attributes")]
        [Tooltip("Performance under pressure (time, score)")]
        [Range(0f, 1f)] public float composure = 0.5f;

        [Tooltip("Aggressiveness in physical play")]
        [Range(0f, 1f)] public float aggression = 0.5f;

        [Tooltip("Tendency for creative/unexpected plays")]
        [Range(0f, 1f)] public float creativity = 0.5f;

        [Header("Specialties")]
        public bool foulDrawingExpert = false;     // Good at drawing fouls
        public bool screenSpecialist = false;       // Effective at setting screens
        public bool wristGrabber = false;           // Grabs defender wrists (risky)
        public bool counterAttackThreat = false;    // Dangerous in fast breaks
        public bool clutchPerformer = false;        // Better in critical moments
        public bool defensiveAnchor = false;        // Strong defender

        [Header("Goalkeeper Specific (if applicable)")]
        [Range(0f, 1f)] public float reflexes = 0.5f;
        [Range(0f, 1f)] public float positioning_GK = 0.5f;  // GK-specific positioning
        [Range(0f, 1f)] public float handling = 0.5f;
        [Range(0f, 1f)] public float distribution = 0.5f;   // Passing out from goal

        /// <summary>
        /// Calculate overall rating (0-100).
        /// </summary>
        public float GetOverallRating()
        {
            float physicalAvg = (swimSpeed + acceleration + endurance + strength + verticalReach) / 5f;
            float technicalAvg = (shotPower + shotAccuracy + passAccuracy + ballControl + catchAbility) / 5f;
            float tacticalAvg = (gameReading + positioning + anticipation + decisionSpeed) / 4f;
            float mentalAvg = (composure + aggression + creativity) / 3f;

            float overall = (physicalAvg + technicalAvg + tacticalAvg + mentalAvg) / 4f;

            // Bonus for specialties
            int specialtyCount = 0;
            if (foulDrawingExpert) specialtyCount++;
            if (screenSpecialist) specialtyCount++;
            if (counterAttackThreat) specialtyCount++;
            if (clutchPerformer) specialtyCount++;
            if (defensiveAnchor) specialtyCount++;

            overall += specialtyCount * 0.02f; // +2% per specialty

            return Mathf.Clamp01(overall) * 100f;
        }

        /// <summary>
        /// Get actual swim speed in m/s based on attribute.
        /// </summary>
        public float GetActualSwimSpeed()
        {
            // Map 0-1 attribute to realistic swim speeds (1.0 - 2.0 m/s)
            return Mathf.Lerp(1.0f, 2.0f, swimSpeed);
        }

        /// <summary>
        /// Get shot power multiplier.
        /// </summary>
        public float GetShotPowerMultiplier()
        {
            return Mathf.Lerp(0.7f, 1.5f, shotPower);
        }

        /// <summary>
        /// Get shot accuracy variance (lower is more accurate).
        /// </summary>
        public float GetShotAccuracyVariance()
        {
            return Mathf.Lerp(3f, 0.5f, shotAccuracy); // Degrees of deviation
        }

        /// <summary>
        /// Get fatigue rate (how quickly player tires).
        /// </summary>
        public float GetFatigueRate()
        {
            return Mathf.Lerp(1.5f, 0.5f, endurance);
        }

        /// <summary>
        /// Check if player succeeds at a skill check.
        /// </summary>
        public bool SkillCheck(float attributeValue, float difficulty = 0.5f)
        {
            // Higher attribute = better chance
            // Higher difficulty = lower chance
            float threshold = Mathf.Lerp(0.3f, 0.9f, difficulty);
            return attributeValue >= threshold || Random.value < attributeValue;
        }

        /// <summary>
        /// Apply fatigue effect to attributes.
        /// </summary>
        public void ApplyFatigue(ref float currentFatigue)
        {
            // Fatigue reduces physical and mental attributes
            // This would be applied during runtime, modifying effective attributes
            // Phase 5 feature
        }

        /// <summary>
        /// Generate random attributes for quick testing.
        /// </summary>
        [ContextMenu("Generate Random Attributes")]
        public void GenerateRandomAttributes()
        {
            // Physical
            swimSpeed = Random.Range(0.3f, 0.9f);
            acceleration = Random.Range(0.3f, 0.9f);
            endurance = Random.Range(0.3f, 0.9f);
            strength = Random.Range(0.3f, 0.9f);
            verticalReach = Random.Range(0.3f, 0.9f);

            // Technical
            shotPower = Random.Range(0.3f, 0.9f);
            shotAccuracy = Random.Range(0.3f, 0.9f);
            passAccuracy = Random.Range(0.3f, 0.9f);
            ballControl = Random.Range(0.3f, 0.9f);
            catchAbility = Random.Range(0.3f, 0.9f);

            // Tactical
            gameReading = Random.Range(0.3f, 0.9f);
            positioning = Random.Range(0.3f, 0.9f);
            anticipation = Random.Range(0.3f, 0.9f);
            decisionSpeed = Random.Range(0.3f, 0.9f);

            // Mental
            composure = Random.Range(0.3f, 0.9f);
            aggression = Random.Range(0.2f, 0.8f);
            creativity = Random.Range(0.2f, 0.8f);

            // Specialties (20% chance each)
            foulDrawingExpert = Random.value < 0.2f;
            screenSpecialist = Random.value < 0.2f;
            wristGrabber = Random.value < 0.15f;
            counterAttackThreat = Random.value < 0.2f;
            clutchPerformer = Random.value < 0.15f;
            defensiveAnchor = Random.value < 0.2f;

            Debug.Log($"Generated random attributes - Overall: {GetOverallRating():F1}");
        }

        /// <summary>
        /// Create elite player attributes.
        /// </summary>
        [ContextMenu("Make Elite Player")]
        public void MakeElitePlayer()
        {
            swimSpeed = Random.Range(0.7f, 0.95f);
            acceleration = Random.Range(0.7f, 0.95f);
            endurance = Random.Range(0.7f, 0.95f);
            strength = Random.Range(0.7f, 0.95f);
            verticalReach = Random.Range(0.7f, 0.95f);

            shotPower = Random.Range(0.7f, 0.95f);
            shotAccuracy = Random.Range(0.7f, 0.95f);
            passAccuracy = Random.Range(0.7f, 0.95f);
            ballControl = Random.Range(0.7f, 0.95f);
            catchAbility = Random.Range(0.7f, 0.95f);

            gameReading = Random.Range(0.7f, 0.95f);
            positioning = Random.Range(0.7f, 0.95f);
            anticipation = Random.Range(0.7f, 0.95f);
            decisionSpeed = Random.Range(0.7f, 0.95f);

            composure = Random.Range(0.7f, 0.95f);
            aggression = Random.Range(0.5f, 0.9f);
            creativity = Random.Range(0.6f, 0.9f);

            // More likely to have specialties
            foulDrawingExpert = Random.value < 0.4f;
            screenSpecialist = Random.value < 0.4f;
            counterAttackThreat = Random.value < 0.5f;
            clutchPerformer = Random.value < 0.4f;
            defensiveAnchor = Random.value < 0.3f;

            Debug.Log($"Created elite player - Overall: {GetOverallRating():F1}");
        }
    }
}
