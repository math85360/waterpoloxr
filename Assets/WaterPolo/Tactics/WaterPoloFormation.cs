using UnityEngine;
using System.Collections.Generic;
using WaterPolo.Players;

namespace WaterPolo.Tactics
{
    /// <summary>
    /// Types of positioning rules for tactical placement.
    /// Determines how a player positions themselves relative to context.
    /// </summary>
    public enum PositioningRuleType
    {
        FormationBase,        // Fixed position from formation
        IntervalDefense,      // Between two opponents
        PressCarrier,         // Pressure ball carrier
        ZoneCoverage,         // Cover specific zone
        SupportBallCarrier,   // Support teammate with ball
        OppositeWing,         // Move to wing opposite ball
        MarkPlayer,           // Mark specific opponent
        ScreenForTeammate,    // Set screen to free teammate
        CutToGoal,            // Cut towards goal
        DropBack              // Drop back defensively
    }

    /// <summary>
    /// Defines a positioning rule with weight and conditions.
    /// </summary>
    [System.Serializable]
    public class PositioningRule
    {
        public PositioningRuleType ruleType;
        [Range(0f, 1f)] public float weight = 1f;
        public bool requiresBallPossession = false;
        public bool requiresDefending = false;

        public PositioningRule(PositioningRuleType type, float weight = 1f)
        {
            this.ruleType = type;
            this.weight = weight;
        }
    }

    /// <summary>
    /// Defines position and behavior for a specific role in a formation.
    /// </summary>
    [System.Serializable]
    public class FormationPosition
    {
        [Header("Role")]
        public PlayerRole role;
        public string positionName;

        [Header("Base Position")]
        public Vector3 attackPosition;     // Position when attacking
        public Vector3 defensePosition;    // Position when defending
        public float influenceRadius = 3f; // Zone of responsibility

        [Header("Positioning Rules")]
        public List<PositioningRule> attackRules = new List<PositioningRule>();
        public List<PositioningRule> defenseRules = new List<PositioningRule>();

        [Header("Movement")]
        [Range(0f, 1f)] public float mobility = 0.5f; // How much player can deviate from base
        public bool canRotate = true; // Can rotate positions with teammates
    }

    /// <summary>
    /// ScriptableObject defining a complete water polo formation.
    /// Contains positions for all 7 players (6 field + 1 goalkeeper).
    /// </summary>
    [CreateAssetMenu(fileName = "Formation", menuName = "WaterPolo/Formation", order = 1)]
    public class WaterPoloFormation : ScriptableObject
    {
        [Header("Formation Info")]
        public string formationName = "3-3";
        [TextArea(3, 5)]
        public string description;

        [Header("Positions")]
        public FormationPosition[] positions = new FormationPosition[7];

        [Header("Formation Characteristics")]
        [Range(0f, 1f)] public float offensiveBalance = 0.5f; // 0=defensive, 1=offensive
        [Range(0f, 1f)] public float pressureIntensity = 0.5f; // How aggressive
        public bool goodAgainstZone = false;
        public bool goodAgainstMan = false;

        /// <summary>
        /// Get position for a specific role.
        /// </summary>
        public FormationPosition GetPositionForRole(PlayerRole role)
        {
            foreach (var pos in positions)
            {
                if (pos.role == role)
                    return pos;
            }
            return null;
        }

        /// <summary>
        /// Get base position for role in current game phase.
        /// </summary>
        public Vector3 GetBasePosition(PlayerRole role, bool isAttacking)
        {
            FormationPosition pos = GetPositionForRole(role);
            if (pos == null) return Vector3.zero;

            return isAttacking ? pos.attackPosition : pos.defensePosition;
        }

        /// <summary>
        /// Get active positioning rules for role in current phase.
        /// </summary>
        public List<PositioningRule> GetActiveRules(PlayerRole role, bool isAttacking)
        {
            FormationPosition pos = GetPositionForRole(role);
            if (pos == null) return new List<PositioningRule>();

            return isAttacking ? pos.attackRules : pos.defenseRules;
        }

        /// <summary>
        /// Validate formation has all required positions.
        /// </summary>
        public bool ValidateFormation()
        {
            if (positions == null || positions.Length != 7)
            {
                Debug.LogError($"Formation {formationName}: Must have exactly 7 positions!");
                return false;
            }

            // Check for goalkeeper
            bool hasGoalkeeper = false;
            foreach (var pos in positions)
            {
                if (pos.role == PlayerRole.Goalkeeper)
                {
                    hasGoalkeeper = true;
                    break;
                }
            }

            if (!hasGoalkeeper)
            {
                Debug.LogError($"Formation {formationName}: Missing goalkeeper!");
                return false;
            }

            return true;
        }

        private void OnValidate()
        {
            ValidateFormation();
        }
    }
}
