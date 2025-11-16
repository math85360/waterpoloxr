using System.Collections.Generic;
using UnityEngine;

namespace WaterPolo.Core
{
    /// <summary>
    /// Tracks score and statistics for both teams.
    /// Manages goal validation and score updates.
    /// </summary>
    public class ScoreTable : MonoBehaviour
    {
        [Header("Team Configuration")]
        [SerializeField] private string _homeTeamName = "Home";
        [SerializeField] private string _awayTeamName = "Away";

        [Header("Current Score")]
        [SerializeField] private int _homeScore = 0;
        [SerializeField] private int _awayScore = 0;

        [Header("Statistics")]
        [SerializeField] private TeamStats _homeStats = new TeamStats();
        [SerializeField] private TeamStats _awayStats = new TeamStats();

        private List<GoalRecord> _goalHistory = new List<GoalRecord>();
        private MatchState _matchState;

        #region Properties

        public int HomeScore => _homeScore;
        public int AwayScore => _awayScore;
        public string HomeTeamName => _homeTeamName;
        public string AwayTeamName => _awayTeamName;
        public TeamStats HomeStats => _homeStats;
        public TeamStats AwayStats => _awayStats;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _matchState = GetComponent<MatchState>();
        }

        #endregion

        #region Goal Management

        /// <summary>
        /// Register a goal for a team.
        /// Validates goal based on match state.
        /// </summary>
        public bool RegisterGoal(string team, MonoBehaviour scorer)
        {
            // Validate match state
            if (_matchState != null && !_matchState.CanShoot)
            {
                Debug.LogWarning($"Cannot score goal in state {_matchState.CurrentState}");
                EventBus.Instance.Publish(new GoalInvalidatedEvent($"Invalid match state: {_matchState.CurrentState}"));
                return false;
            }

            // Update score
            bool isHomeTeam = team == _homeTeamName;
            if (isHomeTeam)
            {
                _homeScore++;
            }
            else
            {
                _awayScore++;
            }

            // Record goal
            GoalRecord record = new GoalRecord
            {
                Team = team,
                Scorer = scorer,
                Quarter = GetComponent<GameClock>()?.CurrentQuarter ?? 1,
                TimeRemaining = GetComponent<GameClock>()?.QuarterTimeRemaining ?? 0f,
                HomeScore = _homeScore,
                AwayScore = _awayScore
            };
            _goalHistory.Add(record);

            // Update stats
            if (isHomeTeam)
            {
                _homeStats.Goals++;
            }
            else
            {
                _awayStats.Goals++;
            }

            // Publish events
            int newScore = isHomeTeam ? _homeScore : _awayScore;
            EventBus.Instance.Publish(new GoalScoredEvent(team, scorer, newScore));
            EventBus.Instance.Publish(new GoalValidatedEvent(scorer));

            // Transition match state
            if (_matchState != null)
            {
                _matchState.TransitionToState(MatchStateType.GOAL_SCORED);
            }

            Debug.Log($"Goal scored! {_homeTeamName} {_homeScore} - {_awayScore} {_awayTeamName}");

            return true;
        }

        #endregion

        #region Statistics

        public void RecordShot(string team, bool onTarget)
        {
            bool isHomeTeam = team == _homeTeamName;

            if (isHomeTeam)
            {
                _homeStats.Shots++;
                if (onTarget) _homeStats.ShotsOnTarget++;
            }
            else
            {
                _awayStats.Shots++;
                if (onTarget) _awayStats.ShotsOnTarget++;
            }
        }

        public void RecordPass(string team, bool successful)
        {
            bool isHomeTeam = team == _homeTeamName;

            if (isHomeTeam)
            {
                _homeStats.Passes++;
                if (successful) _homeStats.SuccessfulPasses++;
            }
            else
            {
                _awayStats.Passes++;
                if (successful) _awayStats.SuccessfulPasses++;
            }
        }

        public void RecordFoul(string team)
        {
            bool isHomeTeam = team == _homeTeamName;

            if (isHomeTeam)
            {
                _homeStats.Fouls++;
            }
            else
            {
                _awayStats.Fouls++;
            }
        }

        public void RecordExclusion(string team)
        {
            bool isHomeTeam = team == _homeTeamName;

            if (isHomeTeam)
            {
                _homeStats.Exclusions++;
            }
            else
            {
                _awayStats.Exclusions++;
            }
        }

        #endregion

        #region Queries

        public string GetWinner()
        {
            if (_homeScore > _awayScore) return _homeTeamName;
            if (_awayScore > _homeScore) return _awayTeamName;
            return "Draw";
        }

        public int GetScoreDifference()
        {
            return Mathf.Abs(_homeScore - _awayScore);
        }

        public List<GoalRecord> GetGoalHistory()
        {
            return new List<GoalRecord>(_goalHistory);
        }

        #endregion

        #region Reset

        public void ResetScore()
        {
            _homeScore = 0;
            _awayScore = 0;
            _homeStats = new TeamStats();
            _awayStats = new TeamStats();
            _goalHistory.Clear();

            Debug.Log("Score table reset");
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Statistics tracked for each team.
    /// </summary>
    [System.Serializable]
    public class TeamStats
    {
        public int Goals = 0;
        public int Shots = 0;
        public int ShotsOnTarget = 0;
        public int Passes = 0;
        public int SuccessfulPasses = 0;
        public int Fouls = 0;
        public int Exclusions = 0;
        public int Interceptions = 0;
        public int Steals = 0;

        public float ShotAccuracy => Shots > 0 ? (float)ShotsOnTarget / Shots : 0f;
        public float PassAccuracy => Passes > 0 ? (float)SuccessfulPasses / Passes : 0f;
    }

    /// <summary>
    /// Record of a single goal scored in the match.
    /// </summary>
    [System.Serializable]
    public class GoalRecord
    {
        public string Team;
        public MonoBehaviour Scorer;
        public int Quarter;
        public float TimeRemaining;
        public int HomeScore;
        public int AwayScore;
        public float Timestamp = Time.time;
    }

    #endregion
}
