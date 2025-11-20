using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Interfaces;
using FlawsFightNight.Core.Models.MatchLogs;
using FlawsFightNight.Core.Models.TieBreakers;

namespace FlawsFightNight.Core.Models.Tournaments
{
    public class NormalRoundRobinTournament : TournamentBase, IRoundBased, ITeamLocking, ITieBreakerRankSystem
    {
        public int CurrentRound { get; set; } = 0;
        public int? TotalRounds { get; set; } = null;
        public bool IsRoundComplete { get; set; } = false;
        public bool IsRoundLockedIn { get; set; } = false;

        public bool IsTeamsLocked { get; set; } = false;
        public bool CanTeamsBeLocked { get; set; } = false;
        public bool CanTeamsBeUnlocked { get; set; } = false;

        public ITieBreakerRule TieBreakerRule { get; set; } = new TraditionalTieBreaker();

        public bool IsDoubleRoundRobin { get; set; } = true;


        public NormalRoundRobinTournament(string id, string name, int teamSize) : base(id, name, teamSize)
        {
            Type = TournamentType.NormalRoundRobin;
            MatchLog = new NormalRoundRobinMatchLog();
        }

        public override bool IsReadyToStart()
        {
            return IsTeamsLocked == true && IsRunning == false && Teams.Count >= 3;
        }

        public override void Start()
        {
            // TODO Test Normal Round Robin specific Start logic
            CurrentRound = 1;
            IsRunning = true;
            CanTeamsBeLocked = false;
            CanTeamsBeUnlocked = false;
        }

        public override bool IsReadyToEnd()
        {
            // A Normal Round Robin tournament ends when all rounds are complete and locked in
            return CurrentRound >= TotalRounds && IsRoundComplete && IsRoundLockedIn;
        }

        public override void End()
        {
            // TODO Test Normal Round Robin specific End logic here
            IsRunning = false;
            IsTeamsLocked = false;
            CanTeamsBeUnlocked = false;
            CanTeamsBeLocked = true;
            IsRoundComplete = false;
            IsRoundLockedIn = false;
        }

        public override string GetFormattedType() => "Normal Round Robin";

        public override bool CanDelete()
        {
            if (!IsRunning && !IsTeamsLocked)
            {
                return true;
            }

            return false;
        }

        public override bool CanAcceptNewTeams()
        {
            return !IsRunning && !IsTeamsLocked;
        }

        public bool DoesRoundContainByeMatch()
        {
            // TODO Test Normal RR DoesRoundContainByeMatch logic here
            if (MatchLog is NormalRoundRobinMatchLog rrLog)
            {
                var matchesThisRound = rrLog.GetAllActiveMatches().Where(m => m.RoundNumber == CurrentRound);
                return matchesThisRound.Any(m => m.IsByeMatch);
            }
            return false;
        }

        public void AdvanceRound()
        {
            // TODO Test Normal RR AdvanceRound logic here
            CurrentRound++;
            IsRoundComplete = false;
            IsRoundLockedIn = false;
        }

        public void SetRanksByTieBreakerLogic()
        {
            // TODO Transfer Tie Breaker application logic here

            // Sort base order by W-L and total score for initial grouping
            Teams = Teams
                .OrderByDescending(t => t.Wins)
                .ThenBy(t => t.Losses)
                .ThenByDescending(t => t.TotalScore)
                .ToList();

            // Group teams by exact W-L
            var groupedByRecord = Teams
                .GroupBy(t => new { t.Wins, t.Losses })
                .OrderByDescending(g => g.Key.Wins)
                .ThenBy(g => g.Key.Losses);

            // Resolve each group using the Tie Breaker Rule
            var resolvedTeamsList = new List<Team>();
            foreach (var group in groupedByRecord)
            {
                var tiedTeams = group.Select(e => e.Name).ToList();
                if (tiedTeams.Count > 1)
                {
                    // Work only with tiedTeams
                    while (tiedTeams.Count > 0)
                    {
                        // Resolve tie and get a winner
                        var (_, winner) = TieBreakerRule.ResolveTie(tiedTeams, MatchLog);
                        var winnerTeam = group.First(e => e.Name == winner);
                        resolvedTeamsList.Add(winnerTeam);

                        // Remove the winner so it's not picked again
                        tiedTeams.Remove(winner);
                    }
                }
                else
                {
                    // No tie, just add the single team
                    resolvedTeamsList.AddRange(group);
                }
            }
            // Assign ranks in order after tie-resolution
            for (int i = 0; i < resolvedTeamsList.Count; i++)
            {
                resolvedTeamsList[i].Rank = i + 1;
            }
            // Update the Teams list with the resolved order
            Teams = resolvedTeamsList;
        }
    }
}

