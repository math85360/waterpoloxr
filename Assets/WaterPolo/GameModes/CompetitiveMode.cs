using UnityEngine;
using WaterPolo.Core;

namespace WaterPolo.GameModes
{
    /// <summary>
    /// Standard competitive water polo mode.
    /// Full rules: 4 quarters, shot clock, referee, full roster.
    /// </summary>
    public class CompetitiveMode : GameMode
    {
        [Header("Match Configuration")]
        [SerializeField] private int _quarterCount = 4;
        [SerializeField] private float _quarterDuration = 480f; // 8 minutes
        [SerializeField] private bool _enableShotClock = true;
        [SerializeField] private bool _enableReferee = true;

        #region Setup & Start

        public override void Setup()
        {
            // Configure game clock
            if (_gameClock != null)
            {
                // Game clock configuration would be set here
                // In Unity, this would be done via Inspector or SetGameClockConfig method
            }

            // Setup scoreboard
            if (_scoreTable != null)
            {
                _scoreTable.ResetScore();
            }

            // Subscribe to events
            EventBus.Instance.Subscribe<GoalScoredEvent>(OnGoalScored);
            EventBus.Instance.Subscribe<MatchEndedEvent>(OnMatchEnded);

            Debug.Log("Competitive Mode setup complete");
        }

        public override void StartGame()
        {
            Activate();

            // Start match
            if (_matchState != null)
            {
                _matchState.StartMatch();
            }

            if (_gameClock != null)
            {
                _gameClock.StartClock();
            }

            Debug.Log("Competitive match started");
        }

        #endregion

        #region Game Logic

        protected override void UpdateGameLogic()
        {
            // Check for match end
            if (_gameClock != null)
            {
                if (_gameClock.CurrentQuarter > _quarterCount)
                {
                    EndGame();
                }
            }
        }

        public override bool CheckWinCondition()
        {
            // Win condition: higher score at end of regulation
            if (_scoreTable == null) return false;

            if (_gameClock != null && _gameClock.CurrentQuarter > _quarterCount)
            {
                return _scoreTable.GetScoreDifference() != 0; // No draw check for now
            }

            return false;
        }

        public override void OnGoalScored(GoalScoredEvent goal)
        {
            // Standard goal handling (already done by ScoreTable)
            // Could add mode-specific celebrations, camera angles, etc.

            Debug.Log($"Goal! {goal.ScoringTeam} now has {goal.NewScore} goals");
        }

        #endregion

        #region End Game

        public override void EndGame()
        {
            if (_isCompleted) return;

            Complete();

            // Unsubscribe from events
            EventBus.Instance.Unsubscribe<GoalScoredEvent>(OnGoalScored);
            EventBus.Instance.Unsubscribe<MatchEndedEvent>(OnMatchEnded);

            // Display final results
            if (_scoreTable != null)
            {
                string winner = _scoreTable.GetWinner();
                Debug.Log($"=== MATCH FINAL ===");
                Debug.Log($"{_scoreTable.HomeTeamName}: {_scoreTable.HomeScore}");
                Debug.Log($"{_scoreTable.AwayTeamName}: {_scoreTable.AwayScore}");
                Debug.Log($"Winner: {winner}");
            }
        }

        private void OnMatchEnded(MatchEndedEvent evt)
        {
            EndGame();
        }

        #endregion
    }
}
