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
    public class OpenRoundRobinTournament : TournamentBase, ITeamLocking, ITieBreaker
    {
        public bool IsTeamsLocked { get; set; } = false;
        public bool CanTeamsBeLocked { get; set; } = false;
        public bool CanTeamsBeUnlocked { get; set; } = false;
        public ITieBreakerRule TieBreakerRule { get; set; } = new TraditionalTieBreaker();

        public OpenRoundRobinTournament()
        {
            Type = TournamentType.OpenRoundRobin;
            MatchLog = new OpenRoundRobinMatchLog();
        }

        public override bool IsReadyToStart()
        {
            return IsTeamsLocked == true && IsRunning == false && Teams.Count >= 3;
        }

        public override void Start()
        {
            // TODO Add Open Round Robin specific start logic here
        }

        public override bool IsReadyToEnd()
        {
            return MatchLog.GetAllActiveMatches().Count == 0 && IsRunning;
        }

        public override void End()
        {
            // TODO Add Open Round Robin specific end logic here
        }

        public override string GetFormattedType() => "Open Round Robin";

        public void SetRanksByTieBreakerLogic()
        {
            // TODO Transfer Tie Breaker application logic here
        }
    }
}
