using UnityEngine;
using System.Collections.Generic;
using WaterPolo.Players;

namespace WaterPolo.Tactics
{
    /// <summary>
    /// Manages formation positioning for a team.
    /// Calculates target positions based on formation, game context, and positioning rules.
    /// </summary>
    public class FormationManager : MonoBehaviour
    {
        [Header("Formation")]
        [SerializeField] private WaterPoloFormation _currentFormation;
        [SerializeField] private Transform _ownGoal;
        [SerializeField] private Transform _opponentGoal;

        [Header("Context")]
        [SerializeField] private bool _isAttacking = false;
        [SerializeField] private Vector3 _ballPosition;
        [SerializeField] private WaterPoloPlayer _ballCarrier;

        [Header("Team")]
        [SerializeField] private List<WaterPoloPlayer> _teamPlayers = new List<WaterPoloPlayer>();

        private Dictionary<PlayerRole, Vector3> _calculatedPositions = new Dictionary<PlayerRole, Vector3>();

        #region Properties

        public WaterPoloFormation CurrentFormation => _currentFormation;
        public bool IsAttacking => _isAttacking;

        #endregion

        #region Formation Management

        /// <summary>
        /// Set the active formation.
        /// </summary>
        public void SetFormation(WaterPoloFormation formation)
        {
            if (formation == null)
            {
                Debug.LogWarning("Cannot set null formation!");
                return;
            }

            if (!formation.ValidateFormation())
            {
                Debug.LogError($"Formation {formation.formationName} is invalid!");
                return;
            }

            _currentFormation = formation;
            RecalculatePositions();

            Debug.Log($"Formation changed to: {formation.formationName}");
        }

        /// <summary>
        /// Set attacking/defending phase.
        /// </summary>
        public void SetAttackingPhase(bool attacking)
        {
            if (_isAttacking != attacking)
            {
                _isAttacking = attacking;
                RecalculatePositions();
            }
        }

        #endregion

        #region Position Calculation

        /// <summary>
        /// Recalculate all positions based on current formation and context.
        /// </summary>
        public void RecalculatePositions()
        {
            if (_currentFormation == null) return;

            _calculatedPositions.Clear();

            foreach (var player in _teamPlayers)
            {
                if (player == null) continue;

                Vector3 targetPos = CalculatePositionForPlayer(player);
                _calculatedPositions[player.Role] = targetPos;
                player.SetTargetPosition(targetPos);
            }
        }

        /// <summary>
        /// Calculate target position for a specific player.
        /// </summary>
        private Vector3 CalculatePositionForPlayer(WaterPoloPlayer player)
        {
            if (_currentFormation == null || player == null)
                return player.transform.position;

            // Get base position from formation
            Vector3 basePosition = _currentFormation.GetBasePosition(player.Role, _isAttacking);

            // Transform to world space relative to own goal
            Vector3 worldPosition = TransformFormationPosition(basePosition);

            // Apply positioning rules
            List<PositioningRule> rules = _currentFormation.GetActiveRules(player.Role, _isAttacking);
            Vector3 modifiedPosition = ApplyPositioningRules(worldPosition, player, rules);

            return modifiedPosition;
        }

        /// <summary>
        /// Transform formation position (relative coordinates) to world position.
        /// </summary>
        private Vector3 TransformFormationPosition(Vector3 formationPos)
        {
            if (_ownGoal == null) return formationPos;

            // Formation coordinates are relative to own goal
            // X = left/right, Y = up/down, Z = forward/back (towards opponent)
            Vector3 goalPosition = _ownGoal.position;
            Vector3 goalForward = _opponentGoal != null ?
                (_opponentGoal.position - goalPosition).normalized : Vector3.forward;

            Vector3 goalRight = Vector3.Cross(Vector3.up, goalForward);

            Vector3 worldPos = goalPosition;
            worldPos += goalForward * formationPos.z;
            worldPos += goalRight * formationPos.x;
            worldPos += Vector3.up * formationPos.y;

            return worldPos;
        }

        /// <summary>
        /// Apply positioning rules to modify base position.
        /// </summary>
        private Vector3 ApplyPositioningRules(Vector3 basePosition, WaterPoloPlayer player, List<PositioningRule> rules)
        {
            if (rules == null || rules.Count == 0)
                return basePosition;

            Vector3 accumulatedOffset = Vector3.zero;
            float totalWeight = 0f;

            foreach (var rule in rules)
            {
                // Check if rule applies in current context
                if (rule.requiresBallPossession && _ballCarrier != player)
                    continue;
                if (rule.requiresDefending && _isAttacking)
                    continue;

                Vector3 ruleOffset = CalculateRuleOffset(rule, player, basePosition);
                accumulatedOffset += ruleOffset * rule.weight;
                totalWeight += rule.weight;
            }

            if (totalWeight > 0)
            {
                return basePosition + (accumulatedOffset / totalWeight);
            }

            return basePosition;
        }

        /// <summary>
        /// Calculate position offset based on specific rule type.
        /// </summary>
        private Vector3 CalculateRuleOffset(PositioningRule rule, WaterPoloPlayer player, Vector3 basePos)
        {
            switch (rule.ruleType)
            {
                case PositioningRuleType.FormationBase:
                    return Vector3.zero; // No offset, use base

                case PositioningRuleType.SupportBallCarrier:
                    if (_ballCarrier != null && _ballCarrier != player)
                    {
                        // Move towards ball carrier
                        Vector3 toBallCarrier = _ballCarrier.transform.position - basePos;
                        return toBallCarrier.normalized * 2f;
                    }
                    break;

                case PositioningRuleType.PressCarrier:
                    if (_ballCarrier != null && _ballCarrier.TeamName != player.TeamName)
                    {
                        // Move aggressively towards opponent with ball
                        Vector3 toOpponent = _ballCarrier.transform.position - player.transform.position;
                        return toOpponent.normalized * 3f;
                    }
                    break;

                case PositioningRuleType.OppositeWing:
                    if (_ballPosition != Vector3.zero)
                    {
                        // Determine which side ball is on, move to opposite
                        Vector3 goalToGoal = _opponentGoal.position - _ownGoal.position;
                        Vector3 right = Vector3.Cross(Vector3.up, goalToGoal).normalized;

                        float ballSide = Vector3.Dot(_ballPosition - _ownGoal.position, right);
                        return -right * ballSide * 2f;
                    }
                    break;

                case PositioningRuleType.CutToGoal:
                    if (_opponentGoal != null)
                    {
                        // Move towards opponent goal
                        Vector3 toGoal = _opponentGoal.position - basePos;
                        return toGoal.normalized * 1.5f;
                    }
                    break;

                case PositioningRuleType.DropBack:
                    if (_ownGoal != null)
                    {
                        // Move back towards own goal
                        Vector3 toOwnGoal = _ownGoal.position - basePos;
                        return toOwnGoal.normalized * 2f;
                    }
                    break;

                // More rules will be implemented in Phase 3
            }

            return Vector3.zero;
        }

        #endregion

        #region Context Updates

        /// <summary>
        /// Update ball position context.
        /// </summary>
        public void UpdateBallPosition(Vector3 position)
        {
            _ballPosition = position;
        }

        /// <summary>
        /// Update ball carrier context.
        /// </summary>
        public void UpdateBallCarrier(WaterPoloPlayer carrier)
        {
            _ballCarrier = carrier;

            // Determine if we're attacking or defending
            if (carrier != null)
            {
                _isAttacking = (carrier.TeamName == _teamPlayers[0]?.TeamName);
            }
        }

        #endregion

        #region Team Management

        /// <summary>
        /// Register a player with this formation manager.
        /// </summary>
        public void RegisterPlayer(WaterPoloPlayer player)
        {
            if (!_teamPlayers.Contains(player))
            {
                _teamPlayers.Add(player);
                RecalculatePositions();
            }
        }

        /// <summary>
        /// Unregister a player.
        /// </summary>
        public void UnregisterPlayer(WaterPoloPlayer player)
        {
            _teamPlayers.Remove(player);
            RecalculatePositions();
        }

        /// <summary>
        /// Get all players in formation.
        /// </summary>
        public List<WaterPoloPlayer> GetPlayers()
        {
            return new List<WaterPoloPlayer>(_teamPlayers);
        }

        #endregion

        #region Query

        /// <summary>
        /// Get calculated position for a specific role.
        /// </summary>
        public Vector3 GetPositionForRole(PlayerRole role)
        {
            if (_calculatedPositions.ContainsKey(role))
                return _calculatedPositions[role];

            return Vector3.zero;
        }

        #endregion

        #region Debug

        private void OnDrawGizmosSelected()
        {
            if (_currentFormation == null) return;

            // Draw formation positions
            foreach (var kvp in _calculatedPositions)
            {
                Gizmos.color = _isAttacking ? Color.red : Color.blue;
                Gizmos.DrawWireSphere(kvp.Value, 0.3f);

                // Draw role label
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(kvp.Value + Vector3.up * 0.5f, kvp.Key.ToString());
                #endif
            }

            // Draw goal connections
            if (_ownGoal != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(_ownGoal.position, 0.5f);
            }

            if (_opponentGoal != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(_opponentGoal.position, 0.5f);
            }
        }

        #endregion
    }
}
