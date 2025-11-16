using UnityEngine;
using WaterPolo.Core;
using WaterPolo.Players;

namespace WaterPolo.GameModes
{
    /// <summary>
    /// Keep Away / Passe à 10 game mode.
    /// Goal: Complete 10 consecutive passes without interception.
    /// No shooting, no goals - pure passing practice.
    /// </summary>
    public class KeepAwayMode : GameMode
    {
        [Header("Mode Configuration")]
        [SerializeField] private int _targetPassCount = 10;
        [SerializeField] private float _timeLimit = 180f; // 3 minutes
        [SerializeField] private bool _resetOnInterception = true;

        [Header("Current State")]
        [SerializeField] private int _currentPassCount = 0;
        [SerializeField] private string _passingTeam = "";
        [SerializeField] private float _timeRemaining = 0f;

        [Header("Scoring")]
        [SerializeField] private int _roundsWonHome = 0;
        [SerializeField] private int _roundsWonAway = 0;

        private WaterPoloPlayer _lastBallCarrier = null;

        #region Setup & Start

        public override void Setup()
        {
            _modeName = "Keep Away (Passe à 10)";
            _description = $"Complete {_targetPassCount} passes without interception";

            _currentPassCount = 0;
            _timeRemaining = _timeLimit;

            // Subscribe to events
            EventBus.Instance.Subscribe<BallPossessionChangedEvent>(OnBallPossessionChanged);

            Debug.Log($"Keep Away Mode setup: {_targetPassCount} passes to win");
        }

        public override void StartGame()
        {
            Activate();

            if (_matchState != null)
            {
                _matchState.TransitionToState(MatchStateType.PLAYING);
            }

            Debug.Log("Keep Away started - make your passes count!");
        }

        #endregion

        #region Game Logic

        protected override void UpdateGameLogic()
        {
            // Update timer
            _timeRemaining -= Time.deltaTime;

            if (_timeRemaining <= 0f)
            {
                OnTimeExpired();
            }
        }

        public override bool CheckWinCondition()
        {
            return _currentPassCount >= _targetPassCount;
        }

        public override void OnGoalScored(GoalScoredEvent goal)
        {
            // No goals in Keep Away mode - ignore
            Debug.Log("No goals in Keep Away mode!");
        }

        #endregion

        #region Event Handling

        private void OnBallPossessionChanged(BallPossessionChangedEvent evt)
        {
            if (evt.NewOwner == null)
            {
                // Ball dropped - reset
                if (_resetOnInterception)
                {
                    ResetPassCount("Ball dropped");
                }
                return;
            }

            WaterPoloPlayer newCarrier = evt.NewOwner as WaterPoloPlayer;
            if (newCarrier == null) return;

            // Check if it's a pass (same team) or interception (different team)
            if (_lastBallCarrier != null)
            {
                if (newCarrier.TeamName == _lastBallCarrier.TeamName)
                {
                    // Successful pass
                    OnSuccessfulPass(newCarrier);
                }
                else
                {
                    // Interception by opponent
                    OnInterception(newCarrier);
                }
            }
            else
            {
                // First possession
                _passingTeam = newCarrier.TeamName;
            }

            _lastBallCarrier = newCarrier;
        }

        private void OnSuccessfulPass(WaterPoloPlayer receiver)
        {
            _currentPassCount++;

            Debug.Log($"Pass #{_currentPassCount} for {receiver.TeamName} - {_targetPassCount - _currentPassCount} to go!");

            // Check win
            if (CheckWinCondition())
            {
                OnRoundWon(_passingTeam);
            }
        }

        private void OnInterception(WaterPoloPlayer interceptor)
        {
            Debug.Log($"INTERCEPTION by {interceptor.TeamName}! {_passingTeam} had {_currentPassCount} passes.");

            ResetPassCount($"Intercepted by {interceptor.TeamName}");
            _passingTeam = interceptor.TeamName;
        }

        #endregion

        #region Round Management

        private void ResetPassCount(string reason)
        {
            _currentPassCount = 0;
            Debug.Log($"Pass count reset: {reason}");
        }

        private void OnRoundWon(string team)
        {
            Debug.Log($"=== ROUND WON by {team}! ===");

            // Update score
            if (_scoreTable != null)
            {
                _scoreTable.RegisterGoal(team, _lastBallCarrier);
            }

            if (team == _scoreTable?.HomeTeamName)
            {
                _roundsWonHome++;
            }
            else
            {
                _roundsWonAway++;
            }

            // Reset for next round
            ResetPassCount($"{team} completed {_targetPassCount} passes");

            // Check if mode should end (could play multiple rounds)
            // For now, end after first round win
            EndGame();
        }

        private void OnTimeExpired()
        {
            Debug.Log($"TIME EXPIRED! {_passingTeam} had {_currentPassCount} passes.");
            EndGame();
        }

        #endregion

        #region End Game

        public override void EndGame()
        {
            if (_isCompleted) return;

            Complete();

            EventBus.Instance.Unsubscribe<BallPossessionChangedEvent>(OnBallPossessionChanged);

            Debug.Log($"=== KEEP AWAY ENDED ===");
            Debug.Log($"Rounds won - Home: {_roundsWonHome}, Away: {_roundsWonAway}");
        }

        #endregion
    }
}
