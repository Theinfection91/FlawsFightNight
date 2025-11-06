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
    public class NormalRoundRobinTournament : TournamentBase, IRoundBased, ITeamLocking, ITieBreaker
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


        public NormalRoundRobinTournament()
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
            // TODO Transfer Normal Round Robin specific Start logic here
        }

        public override bool IsReadyToEnd()
        {
            // A Normal Round Robin tournament ends when all rounds are complete and locked in
            return CurrentRound >= TotalRounds && IsRoundComplete && IsRoundLockedIn;
        }

        public override void End()
        {
            // TODO Transfer Normal Round Robin specific End logic here
        }

        public override string GetFormattedType() => "Normal Round Robin";

        public void AdvanceRound()
        {
            // TODO Transfer Normal RR AdvanceRound logic here
        }

        public void ApplyTieBreaker()
        {
            // TODO Transfer Tie Breaker application logic here
        }
    }
}
