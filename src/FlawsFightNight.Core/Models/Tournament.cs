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
            Console.WriteLine("SetRanksByTieBreakerLogic: Starting method.");

            // Sort base order by W-L and total score for initial grouping
            Console.WriteLine("SetRanksByTieBreakerLogic: Sorting teams by Wins, Losses, and TotalScore.");
            Teams = Teams
                .OrderByDescending(t => t.Wins)
                .ThenBy(t => t.Losses)
                .ThenByDescending(t => t.TotalScore)
                .ToList();

            Console.WriteLine($"SetRanksByTieBreakerLogic: Teams sorted. Team order: {string.Join(", ", Teams.Select(t => t.Name))}");

            // 2Group teams by exact W-L
            Console.WriteLine("SetRanksByTieBreakerLogic: Grouping teams by exact W-L record.");
            var groupedByRecord = Teams
                .GroupBy(t => new { t.Wins, t.Losses })
                .OrderByDescending(g => g.Key.Wins)
                .ThenBy(g => g.Key.Losses);

            var resolvedTeamsList = new List<Team>();

            // Resolve ties only within exact W-L groups
            foreach (var group in groupedByRecord)
            {
                var tiedTeams = group.Select(e => e.Name).ToList();
                Console.WriteLine($"SetRanksByTieBreakerLogic: Processing group with W-L ({group.Key.Wins}-{group.Key.Losses}). Teams: {string.Join(", ", tiedTeams)}");

                if (tiedTeams.Count > 1)
                {
                    // Work only with tiedTeams
                    while (tiedTeams.Count > 0)
                    {
                        Console.WriteLine($"SetRanksByTieBreakerLogic: Resolving tie among: {string.Join(", ", tiedTeams)}");
                        // Resolve tie and get a winner
                        var (_, winner) = TieBreakerRule.ResolveTie(tiedTeams, MatchLog);
                        Console.WriteLine($"SetRanksByTieBreakerLogic: TieBreakerRule selected winner: {winner}");
                        var winnerTeam = group.First(e => e.Name == winner);

                        resolvedTeamsList.Add(winnerTeam);

                        // Remove the winner so it's not picked again
                        tiedTeams.Remove(winner);
                        Console.WriteLine($"SetRanksByTieBreakerLogic: Removed winner '{winner}' from tiedTeams. Remaining: {string.Join(", ", tiedTeams)}");
                    }
                }
                else
                {
                    // No tie, just add the single team
                    Console.WriteLine($"SetRanksByTieBreakerLogic: No tie in group. Adding team: {tiedTeams[0]}");
                    resolvedTeamsList.AddRange(group);
                }
            }
            // Assign ranks in order after tie-resolution
            Console.WriteLine("SetRanksByTieBreakerLogic: Assigning ranks to resolved teams.");
            for (int i = 0; i < resolvedTeamsList.Count; i++)
            {
                resolvedTeamsList[i].Rank = i + 1;
                Console.WriteLine($"SetRanksByTieBreakerLogic: Assigned Rank {i + 1} to team {resolvedTeamsList[i].Name}");
            }

            Teams = resolvedTeamsList;
            Console.WriteLine("SetRanksByTieBreakerLogic: Method complete. Final team order: " + string.Join(", ", Teams.Select(t => $"{t.Name}(Rank:{t.Rank})")));
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
