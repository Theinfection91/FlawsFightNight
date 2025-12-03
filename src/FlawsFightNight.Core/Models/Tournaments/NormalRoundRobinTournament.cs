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
    public class NormalRoundRobinTournament : Tournament, IRoundBased, IRoundRobinLength, ITeamLocking, ITieBreakerRankSystem
    {
        public override TournamentType Type { get; protected set; } = TournamentType.NormalRoundRobin;
        public int CurrentRound { get; set; } = 0;
        public int? TotalRounds { get; set; } = null;
        public bool IsRoundComplete { get; set; } = false;
        public bool IsRoundLockedIn { get; set; } = false;
        public bool IsTeamsLocked { get; set; } = false;
        public bool CanTeamsBeLocked { get; set; } = false;
        public bool CanTeamsBeUnlocked { get; set; } = false;
        public ITieBreakerRule TieBreakerRule { get; set; } = new TraditionalTieBreaker();
        public bool IsDoubleRoundRobin { get; set; } = true;

        [JsonProperty(TypeNameHandling = TypeNameHandling.Auto)]
        public override MatchLog MatchLog { get; protected set; }

        [JsonConstructor]
        protected NormalRoundRobinTournament() : base() { }

        public NormalRoundRobinTournament(string id, string name, int teamSize) : base(id, name, teamSize)
        {
            MatchLog ??= new NormalRoundRobinMatchLog();
        }

        public override bool CanStart(out ErrorReason errorReason)
        {
            if (IsTeamsLocked == false)
            {
                errorReason = ErrorReasonGenerator.GenerateTeamsNotLockedError();
                return false;
            }
            if (IsRunning)
            {
                errorReason = ErrorReasonGenerator.GenerateIsRunningError();
                return false;
            }
            if (Teams.Count < 3)
            {
                errorReason = ErrorReasonGenerator.GenerateInsufficientTeamsError();
                return false;
            }
            errorReason = null;
            return true;
        }

        public override void Start()
        {
            CurrentRound = 1;
            IsRunning = true;
            CanTeamsBeLocked = false;
            CanTeamsBeUnlocked = false;
        }

        public override bool CanEnd(out ErrorReason errorReason)
        {
            errorReason = null;

            // A Normal Round Robin tournament ends when all rounds are complete and locked in
            if (CurrentRound < TotalRounds)
            {
                errorReason = ErrorReasonGenerator.GenerateSpecific("Not all rounds are complete.");
                return false;
            }
            if (!IsRoundComplete)
            {
                errorReason = ErrorReasonGenerator.GenerateSpecific("Current round is not complete.");
                return false;
            }
            if (!IsRoundLockedIn)
            {
                errorReason = ErrorReasonGenerator.GenerateSpecific("Current round is not locked in.");
                return false;
            }
            return true;
        }
        //{
        //    // A Normal Round Robin tournament ends when all rounds are complete and locked in
        //    return CurrentRound >= TotalRounds && IsRoundComplete && IsRoundLockedIn;
        //}

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
                errorReason = ErrorReasonGenerator.GenerateInsufficientTeamsError();
                return false;
            }
            errorReason = null;
            return !IsRunning && !IsTeamsLocked && CanTeamsBeLocked;
        }

        public bool CanUnlockTeams(out ErrorReason errorReason)
        {
            if (IsRunning)
            {
                errorReason = ErrorReasonGenerator.GenerateIsRunningError();
                return false;
            }
            if (!IsTeamsLocked)
            {
                errorReason = ErrorReasonGenerator.GenerateTeamsAlreadyUnlockedError();
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

        public bool CanRoundComplete()
        {
            if (MatchLog is NormalRoundRobinMatchLog rrLog)
            {
                return rrLog.IsRoundComplete(CurrentRound);
            }
            return false;
        }

        public bool CanLockRound()
        {
            return IsRoundComplete && !IsRoundLockedIn;
        }

        public void LockRound()
        {
            IsRoundLockedIn = true;
        }

        public bool CanUnlockRound()
        {
            return IsRoundLockedIn;
        }

        public void UnlockRound()
        {
            IsRoundLockedIn = false;
        }

        public bool CanAdvanceRound()
        {
            if (MatchLog is NormalRoundRobinMatchLog rrLog)
            {
                // Can advance if the round is locked in, complete, and there are more rounds to play
                return IsRoundLockedIn && rrLog.IsRoundComplete(CurrentRound) && 
                    CurrentRound < TotalRounds;
            }
            return false;
        }

        public void AdvanceRound()
        {
            // Convert any bye matches to post matches before advancing
            if (DoesRoundContainByeMatch() && MatchLog is NormalRoundRobinMatchLog rrLog)
            {
                rrLog.ConvertByeMatch(CurrentRound);
            }

            // Advance to the next round process
            CurrentRound++;
            IsRoundComplete = false;
            IsRoundLockedIn = false;
        }

        public bool DoesRoundContainByeMatch()
        {
            // TODO Test Normal RR DoesRoundContainByeMatch logic here
            if (MatchLog is NormalRoundRobinMatchLog rrLog)
            {
                //var matchesThisRound = rrLog.GetAllActiveMatches().Where(m => m.RoundNumber == CurrentRound);
                //return matchesThisRound.Any(m => m.IsByeMatch);
                foreach (var match in rrLog.MatchesToPlayByRound[CurrentRound])
                {
                    if (match.IsByeMatch)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override void AdjustRanks()
        {
            SetRanksByTieBreakerLogic();
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

