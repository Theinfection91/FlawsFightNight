using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Interfaces;
using FlawsFightNight.Core.Models.TieBreakers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Models
{
    public class Tournament
    {
        // Basic Info Properties
        public string Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public TournamentType Type { get; set; }
        public int TeamSize { get; set; }
        public string TeamSizeFormat => $"{TeamSize}v{TeamSize}";
        public List<Team> Teams { get; set; } = [];
        public bool IsRunning { get; set; } = false;
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        // Team Locking Properties
        public bool IsTeamsLocked { get; set; } = false;
        public bool CanTeamsBeUnlocked { get; set; } = false;
        public bool CanTeamsBeLocked { get; set; } = false;

        // Rounds and Ending Tournament Properties
        public int CurrentRound { get; set; } = 0;
        public int? TotalRounds { get; set; } = null;
        public bool IsRoundComplete { get; set; } = false;
        public bool IsRoundLockedIn { get; set; } = false;
        public bool CanEndRoundRobinTournament => CurrentRound >= TotalRounds && IsRoundComplete && IsRoundLockedIn;

        // Round Robin Specific Properties
        public bool CanEndNormalRoundRobinTournament => CurrentRound >= TotalRounds && IsRoundComplete && IsRoundLockedIn;
        public bool CanEndOpenRoundRobinTournament => MatchLog.OpenRoundRobinMatchesToPlay.Count == 0 && IsRunning;
        public ITieBreakerRule TieBreakerRule { get; set; } = new TraditionalTieBreaker();
        public bool IsDoubleRoundRobin { get; set; } = true;
        public RoundRobinMatchType RoundRobinMatchType { get; set; } = RoundRobinMatchType.Normal;

        // Discord Channel ID's for LiveView
        public ulong MatchesChannelId { get; set; } = 0;
        public ulong MatchesMessageId { get; set; } = 0;
        public ulong StandingsChannelId { get; set; } = 0;
        public ulong StandingsMessageId { get; set; } = 0;
        public ulong TeamsChannelId { get; set; } = 0;
        public ulong TeamsMessageId { get; set; } = 0;

        // Match Log to track all matches in the tournament, current and past
        public MatchLog MatchLog { get; set; } = new();

        public Tournament(string name, string? description = null)
        {
            Name = name;
            Description = description;
        }

        public bool IsLadderTournamentReadyToStart()
        {
            // Need at least 3 teams to start a ladder tournament
            return Teams.Count >= 3;
        }

        public void LadderStartTournamentProcess()
        {
            IsRunning = true;
        }

        public void LadderEndTournamentProcess()
        {
            IsRunning = false;

            // Clear any unplayed challenges
            MatchLog.LadderMatchesToPlay.Clear();
        }

        public Team LadderGetRankOneTeam()
        {
            // Return null if no teams exist
            if (Teams == null || Teams.Count == 0)
            {
                return null;
            }

            // Return the team with Rank 1
            return Teams.FirstOrDefault(t => t.Rank == 1);
        }

        public void InitiateStartNormalRoundRobinTournament()
        {
            CurrentRound = 1;
            IsRunning = true;
            CanTeamsBeLocked = false;
            CanTeamsBeUnlocked = false;
        }

        public void InitiateEndNormalRoundRobinTournament()
        {
            IsRunning = false;
            IsTeamsLocked = false;
            CanTeamsBeUnlocked = false;
            CanTeamsBeLocked = true;
            IsRoundComplete = false;
            IsRoundLockedIn = false;
        }

        public void InitiateStartOpenRoundRobinTournament()
        {
            IsRunning = true;
            CanTeamsBeLocked = false;
            CanTeamsBeUnlocked = false;
        }

        public void InitiateEndOpenRoundRobinTournament()
        {
            IsRunning = false;
            IsTeamsLocked = false;
            CanTeamsBeUnlocked = false;
            CanTeamsBeLocked = true;
        }

        #region Ladder Helpers
        public void AddLadderMatchToMatchLog(Match match)
        {
            MatchLog.LadderMatchesToPlay.Add(match);
            // Sort MatchLog by Creation Date, oldest at the top
            MatchLog.LadderMatchesToPlay = MatchLog.LadderMatchesToPlay
                .OrderBy(m => m.CreatedOn)
                .ToList();
        }

        public void DeleteLadderMatchFromMatchLog(Match pendingMatch)
        {
            MatchLog.LadderMatchesToPlay.Remove(pendingMatch);
        }
        #endregion

        #region Round Robin Helpers
        public void SetRanksByTieBreakerLogic()
        {
            // Sort base order by W-L and total score for initial grouping
            Teams = Teams
                .OrderByDescending(t => t.Wins)
                .ThenBy(t => t.Losses)
                .ThenByDescending(t => t.TotalScore)
                .ThenBy(t => t.Name)
                .ToList();

            // 2Group teams by exact W-L
            var groupedByRecord = Teams
                .GroupBy(t => new { t.Wins, t.Losses })
                .OrderByDescending(g => g.Key.Wins)
                .ThenBy(g => g.Key.Losses);

            var resolvedTeamsList = new List<Team>();

            // Resolve ties only within exact W-L groups
            foreach (var group in groupedByRecord)
            {
                var groupTeams = group.OrderByDescending(t => t.TotalScore)
                                      .ThenBy(t => t.Name)
                                      .ToList();

                if (groupTeams.Count == 1)
                {
                    resolvedTeamsList.Add(groupTeams[0]);
                    continue;
                }

                // Multiple teams with same W-L → resolve ties
                var tiedNames = groupTeams.Select(t => t.Name).ToList();
                while (tiedNames.Count > 0)
                {
                    var (_, winnerName) = TieBreakerRule.ResolveTie(tiedNames, MatchLog);
                    var winnerTeam = groupTeams.First(t => t.Name == winnerName);
                    resolvedTeamsList.Add(winnerTeam);
                    tiedNames.Remove(winnerName);
                }
            }

            // Assign ranks in order after tie-resolution
            for (int i = 0; i < resolvedTeamsList.Count; i++)
                resolvedTeamsList[i].Rank = i + 1;

            Teams = resolvedTeamsList;
        }


        public bool DoesRoundContainByeMatch()
        {
            foreach (var match in MatchLog.MatchesToPlayByRound[CurrentRound])
            { 
                if (match.TeamA.Equals("Bye", StringComparison.OrdinalIgnoreCase) || match.TeamB.Equals("Bye", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public Match GetByeMatchInCurrentRound()
        {
            foreach (var match in MatchLog.MatchesToPlayByRound[CurrentRound])
            {
                if (match.TeamA.Equals("Bye", StringComparison.OrdinalIgnoreCase) || match.TeamB.Equals("Bye", StringComparison.OrdinalIgnoreCase))
                {
                    return match;
                }
            }
            return null;
        }
        #endregion

        public string GetFormattedTournamentType()
        {
            switch (this.Type)
            {
                case TournamentType.Ladder:
                    return "Ladder";
                case TournamentType.RoundRobin:
                    switch (RoundRobinMatchType)
                    {
                        case RoundRobinMatchType.Open:
                            return "Open Round Robin";
                        case RoundRobinMatchType.Normal:
                            return "Normal Round Robin";
                    }
                    return "Round Robin";
                case TournamentType.SingleElimination:
                    return "Single Elimination";
                case TournamentType.DoubleElimination:
                    return "Double Elimination";
                default:
                    return "null";
            }
        }
    }
}
