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
        public bool CanEndTournament => CurrentRound >= TotalRounds && IsRoundComplete && IsRoundLockedIn;

        // Round Robin Specific Properties
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

        #region Round Robin Helpers
        public void SetRanksByTieBreakerLogic()
        {
            // Sort teams
            Teams = Teams
            .OrderBy(e => e.Rank)
            .ThenByDescending(e => e.Wins)
            .ThenBy(e => e.Losses)
            .ThenByDescending(e => e.TotalScore)
            //.ThenBy(e => e.TeamName)
            .ToList();

            // Group teams by record
            var groupedTeamsByRecord = Teams
                .GroupBy(e => new { e.Wins, e.Losses })
                .OrderByDescending(g => g.Key.Wins)   // more wins first
                .ThenBy(g => g.Key.Losses);           // fewer losses first

            var resolvedTeamsList = new List<Team>();

            foreach (var group in groupedTeamsByRecord)
            {
                var tiedTeams = group.Select(e => e.Name).ToList();

                if (tiedTeams.Count > 1)
                {
                    // Work only with tiedTeams
                    while (tiedTeams.Count > 0)
                    {
                        // Resolve tie and get a winner
                        var (loser, winner) = TieBreakerRule.ResolveTie(tiedTeams, MatchLog);
                        var winnerTeam = group.First(e => e.Name == winner);

                        resolvedTeamsList.Add(winnerTeam);

                        // Remove the winner so it's not picked again
                        tiedTeams.Remove(winner);
                    }
                }
                else
                {
                    resolvedTeamsList.AddRange(group);
                }
            }
            // Assign ranks after resolution
            for (int i = 0; i < resolvedTeamsList.Count; i++)
                resolvedTeamsList[i].Rank = i + 1;

            // Update the tournament's team list
            Teams = resolvedTeamsList;
        }
        #endregion
    }
}
