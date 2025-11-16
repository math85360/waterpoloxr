using UnityEngine;
using System.Collections.Generic;
using WaterPolo.Core;
using WaterPolo.Tactics;
using WaterPolo.Players;

namespace WaterPolo.AI
{
    /// <summary>
    /// Observes opponent patterns and adapts tactics accordingly.
    /// Learning begins in Q1, adaptations start in Q2+.
    /// As per CLAUDE.md: "Reconnaissance défense (début Q2)"
    /// </summary>
    public class TacticalLearningSystem : MonoBehaviour
    {
        [Header("Observation")]
        [SerializeField] private bool _enableLearning = true;
        [SerializeField] private float _observationConfidenceThreshold = 0.7f;

        [Header("Opponent Analysis")]
        [SerializeField] private DefenseType _detectedDefenseType = DefenseType.ManToMan;
        [SerializeField] private float _defenseConfidence = 0f;

        [Header("Pattern Recognition")]
        [SerializeField] private float _avgShotDistance = 0f;
        [SerializeField] private float _playThroughPivotPercent = 0f;
        [SerializeField] private float _shotFrequency = 0f; // Shots per possession

        [Header("References")]
        [SerializeField] private TeamTactics _ourTeamTactics;
        [SerializeField] private GameClock _gameClock;

        // Observation data
        private List<OpponentPossession> _observedPossessions = new List<OpponentPossession>();
        private int _totalOpponentShots = 0;
        private float _totalShotDistance = 0f;
        private int _passesThroughPivot = 0;
        private int _totalPasses = 0;

        // Defense type indicators
        private Dictionary<DefenseType, float> _defenseTypeScores = new Dictionary<DefenseType, float>();

        private bool _hasAppliedQ2Adaptations = false;

        #region Unity Lifecycle

        private void Awake()
        {
            if (_ourTeamTactics == null)
                _ourTeamTactics = GetComponent<TeamTactics>();

            if (_gameClock == null)
                _gameClock = FindObjectOfType<GameClock>();

            // Initialize defense type scores
            _defenseTypeScores[DefenseType.ManToMan] = 0f;
            _defenseTypeScores[DefenseType.Zone] = 0f;
            _defenseTypeScores[DefenseType.Pressing] = 0f;
        }

        private void Start()
        {
            // Subscribe to events
            EventBus.Instance.Subscribe<QuarterEndedEvent>(OnQuarterEnded);
        }

        private void OnDestroy()
        {
            EventBus.Instance.Unsubscribe<QuarterEndedEvent>(OnQuarterEnded);
        }

        private void Update()
        {
            if (!_enableLearning) return;

            // Continuous observation during match
            if (_gameClock != null && _gameClock.CurrentQuarter == 1)
            {
                // Q1: Observation phase
                ObserveOpponentBehavior();
            }
            else if (_gameClock != null && _gameClock.CurrentQuarter >= 2 && !_hasAppliedQ2Adaptations)
            {
                // Q2+: Apply learned adaptations
                ApplyQ2Adaptations();
            }
        }

        #endregion

        #region Observation

        private void ObserveOpponentBehavior()
        {
            // Observe opponent play patterns
            // In full implementation, this would analyze:
            // - Defensive positioning (zone vs man-to-man)
            // - Shot selection
            // - Pass patterns

            // For Phase 4, simplified observation
            AnalyzeDefenseType();
        }

        /// <summary>
        /// Analyze opponent defensive formation to detect type.
        /// </summary>
        private void AnalyzeDefenseType()
        {
            // Sample analysis every few seconds
            if (Time.frameCount % 180 != 0) return; // ~3 seconds

            // Find opponent players
            WaterPoloPlayer[] allPlayers = FindObjectsOfType<WaterPoloPlayer>();
            List<WaterPoloPlayer> opponentDefenders = new List<WaterPoloPlayer>();
            List<WaterPoloPlayer> ourAttackers = new List<WaterPoloPlayer>();

            string ourTeamName = _ourTeamTactics?.GetComponent<TeamManager>()?.TeamName ?? "";

            foreach (var player in allPlayers)
            {
                if (player.TeamName == ourTeamName)
                {
                    ourAttackers.Add(player);
                }
                else
                {
                    opponentDefenders.Add(player);
                }
            }

            if (opponentDefenders.Count < 3 || ourAttackers.Count < 3)
                return; // Not enough data

            // Analyze defensive behavior
            float zoneLikelihood = AnalyzeForZoneDefense(opponentDefenders, ourAttackers);
            float manToManLikelihood = AnalyzeForManToManDefense(opponentDefenders, ourAttackers);
            float pressingLikelihood = AnalyzeForPressingDefense(opponentDefenders, ourAttackers);

            // Update scores (accumulate over time)
            _defenseTypeScores[DefenseType.Zone] += zoneLikelihood;
            _defenseTypeScores[DefenseType.ManToMan] += manToManLikelihood;
            _defenseTypeScores[DefenseType.Pressing] += pressingLikelihood;

            // Calculate confidence
            UpdateDefenseTypeConfidence();
        }

        private float AnalyzeForZoneDefense(List<WaterPoloPlayer> defenders, List<WaterPoloPlayer> attackers)
        {
            // Zone defense: Defenders stay in positions, don't follow attackers closely
            float zoneLikelihood = 0f;

            // Check if defenders are more spread out and position-based
            foreach (var defender in defenders)
            {
                // Find nearest attacker
                float minDistance = float.MaxValue;
                foreach (var attacker in attackers)
                {
                    float dist = Vector3.Distance(defender.transform.position, attacker.transform.position);
                    if (dist < minDistance)
                        minDistance = dist;
                }

                // If defender is far from all attackers, likely zone
                if (minDistance > 3f)
                    zoneLikelihood += 1f;
            }

            return zoneLikelihood / Mathf.Max(1, defenders.Count);
        }

        private float AnalyzeForManToManDefense(List<WaterPoloPlayer> defenders, List<WaterPoloPlayer> attackers)
        {
            // Man-to-man: Each defender closely marks an attacker
            float manLikelihood = 0f;

            foreach (var defender in defenders)
            {
                // Find nearest attacker
                float minDistance = float.MaxValue;
                foreach (var attacker in attackers)
                {
                    float dist = Vector3.Distance(defender.transform.position, attacker.transform.position);
                    if (dist < minDistance)
                        minDistance = dist;
                }

                // If defender is close to an attacker, likely man-to-man
                if (minDistance < 2f)
                    manLikelihood += 1f;
            }

            return manLikelihood / Mathf.Max(1, defenders.Count);
        }

        private float AnalyzeForPressingDefense(List<WaterPoloPlayer> defenders, List<WaterPoloPlayer> attackers)
        {
            // Pressing: Defenders aggressively close on ball carrier
            // Simplified: Check if multiple defenders near ball

            // This would require ball position tracking
            // Placeholder for Phase 4

            return 0f;
        }

        private void UpdateDefenseTypeConfidence()
        {
            // Find highest score
            DefenseType bestGuess = DefenseType.ManToMan;
            float maxScore = 0f;

            foreach (var kvp in _defenseTypeScores)
            {
                if (kvp.Value > maxScore)
                {
                    maxScore = kvp.Value;
                    bestGuess = kvp.Key;
                }
            }

            // Calculate confidence (normalize scores)
            float totalScore = 0f;
            foreach (var score in _defenseTypeScores.Values)
            {
                totalScore += score;
            }

            if (totalScore > 0)
            {
                _defenseConfidence = maxScore / totalScore;
                _detectedDefenseType = bestGuess;
            }
        }

        #endregion

        #region Q2 Adaptations

        private void ApplyQ2Adaptations()
        {
            _hasAppliedQ2Adaptations = true;

            Debug.Log("=== Q2 TACTICAL ADAPTATIONS ===");
            Debug.Log($"Detected opponent defense: {_detectedDefenseType} (confidence: {_defenseConfidence:F2})");

            // Apply adaptations if confident enough
            if (_defenseConfidence >= _observationConfidenceThreshold && _ourTeamTactics != null)
            {
                _ourTeamTactics.AdaptToOpponentDefense(_detectedDefenseType, _defenseConfidence);
            }
            else
            {
                Debug.Log("Not confident enough to adapt tactics");
            }
        }

        #endregion

        #region Event Handlers

        private void OnQuarterEnded(QuarterEndedEvent evt)
        {
            if (evt.Quarter == 1)
            {
                // End of Q1 - analyze accumulated data
                Debug.Log("=== END Q1 ANALYSIS ===");
                Debug.Log($"Observed {_observedPossessions.Count} opponent possessions");
                Debug.Log($"Defense type confidence: {_detectedDefenseType} ({_defenseConfidence:F2})");
            }
        }

        #endregion

        #region Data Recording

        /// <summary>
        /// Record an opponent shot for analysis.
        /// </summary>
        public void RecordOpponentShot(Vector3 shotPosition, Vector3 goalPosition)
        {
            float distance = Vector3.Distance(shotPosition, goalPosition);
            _totalOpponentShots++;
            _totalShotDistance += distance;
            _avgShotDistance = _totalShotDistance / _totalOpponentShots;

            Debug.Log($"Opponent shot from {distance:F1}m (avg: {_avgShotDistance:F1}m)");
        }

        /// <summary>
        /// Record an opponent pass.
        /// </summary>
        public void RecordOpponentPass(WaterPoloPlayer receiver)
        {
            _totalPasses++;

            if (receiver != null && receiver.Role == PlayerRole.CenterForward)
            {
                _passesThroughPivot++;
                _playThroughPivotPercent = (_passesThroughPivot / (float)_totalPasses) * 100f;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get current understanding of opponent defense.
        /// </summary>
        public DefenseType GetDetectedDefenseType()
        {
            return _detectedDefenseType;
        }

        /// <summary>
        /// Get confidence in defense type detection.
        /// </summary>
        public float GetDefenseConfidence()
        {
            return _defenseConfidence;
        }

        /// <summary>
        /// Force immediate analysis (for testing).
        /// </summary>
        [ContextMenu("Force Analysis")]
        public void ForceAnalysis()
        {
            UpdateDefenseTypeConfidence();
            Debug.Log($"Defense: {_detectedDefenseType}, Confidence: {_defenseConfidence:F2}");
        }

        #endregion
    }

    #region Helper Classes

    /// <summary>
    /// Represents a recorded opponent possession.
    /// </summary>
    public class OpponentPossession
    {
        public float duration;
        public int passCount;
        public bool endedInShot;
        public float shotDistance;
        public bool scoredGoal;

        public OpponentPossession()
        {
            duration = 0f;
            passCount = 0;
            endedInShot = false;
            shotDistance = 0f;
            scoredGoal = false;
        }
    }

    #endregion
}
