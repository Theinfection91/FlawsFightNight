using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Helpers;
using FlawsFightNight.Core.Interfaces;
using FlawsFightNight.Core.Models.MatchLogs;
using FlawsFightNight.Core.Models.TieBreakers;
using Newtonsoft.Json;

namespace FlawsFightNight.Core.Models.Tournaments
{
    public class OpenRoundRobinTournament : Tournament, IRoundRobinLength, ITeamLocking, ITieBreakerRankSystem
    {
        public override TournamentType Type { get; protected set; } = TournamentType.OpenRoundRobin;
        public bool IsTeamsLocked { get; set; } = false;
        public bool CanTeamsBeLocked { get; set; } = false;
        public bool CanTeamsBeUnlocked { get; set; } = false;
        public ITieBreakerRule TieBreakerRule { get; set; } = new TraditionalTieBreaker();
        public bool IsDoubleRoundRobin { get; set; } = true;

        [JsonProperty(TypeNameHandling = TypeNameHandling.Auto)]
        public override MatchLog MatchLog { get; protected set; }

        [JsonConstructor]
        public OpenRoundRobinTournament() : base() { }

        public OpenRoundRobinTournament(string id, string name, int teamSize) : base(id, name, teamSize)
        {
            MatchLog ??= new OpenRoundRobinMatchLog();
        }

        public override bool CanStart()
        {
            return IsTeamsLocked == true && IsRunning == false && Teams.Count >= 3;
        }

        public override void Start()
        {
            // TODO Test Open Round Robin specific start logic here
            IsRunning = true;
            CanTeamsBeLocked = false;
            CanTeamsBeUnlocked = false;
        }

        public override bool CanEnd()
        {
            return MatchLog.GetAllActiveMatches().Count == 0 && IsRunning;
        }

        public override void End()
        {
            // TODO Test Open Round Robin specific end logic here
            IsRunning = false;
            IsTeamsLocked = false;
            CanTeamsBeUnlocked = false;
            CanTeamsBeLocked = true;
        }

        public override string GetFormattedType() => "Open Round Robin";

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

        public bool CanLockTeams(out ErrorReason errorReason)
        {
            if (IsRunning)
            {
                errorReason = ErrorReasonGenerator.GenerateIsRunningError();
                return false;
            }
            if (IsTeamsLocked)
            {
                errorReason = ErrorReasonGenerator.GenerateTeamsAlreadyLockedError();
                return false;
            }
            if (Teams.Count < 3)
            {
                errorReason = ErrorReasonGenerator.GenerateInsufficientTeamsToLockError();
                return false;
            }
            errorReason = null;
            return !IsRunning && !IsTeamsLocked && CanTeamsBeLocked;
        }

        public bool CanUnlockTeams(out ErrorReason errorReason)
        {
            if (IsRunning)
            {
                errorReason = new ErrorReason("Tournament is currently running.");
                return false;
            }
            if (!IsTeamsLocked)
            {
                errorReason = new ErrorReason("Teams are not currently locked.");
                return false;
            }
            //if (!CanTeamsBeUnlocked)
            //{
            //    errorReason = new ErrorReason("Teams cannot be unlocked at this time.");
            //    return false;
            //}
            errorReason = null;
            return !IsRunning && IsTeamsLocked && CanTeamsBeUnlocked;
        }

        public void LockTeams()
        {
            IsTeamsLocked = true;
            CanTeamsBeLocked = false;

            // Allow teams to be unlocked after locking, until tournament starts
            CanTeamsBeUnlocked = true;
        }

        public void UnlockTeams()
        {
            IsTeamsLocked = false;
            CanTeamsBeUnlocked = false;

            // Allow teams to be locked again after unlocking
            CanTeamsBeLocked = true;
        }

        public override void AdjustRanks()
        {
            SetRanksByTieBreakerLogic();
        }

        public void SetRanksByTieBreakerLogic()
        {
            // TODO Test Tie Breaker application logic here

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

            // Resolve ties only within exact W-L groups
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

            Teams = resolvedTeamsList;
        }
    }
}
