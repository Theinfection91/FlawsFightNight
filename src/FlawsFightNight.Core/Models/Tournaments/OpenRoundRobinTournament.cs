using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Models.MatchLogs;
namespace FlawsFightNight.Core.Models.Tournaments
{
    public class OpenRoundRobinTournament : TournamentBase // TODO ITieBreaker and ITeamLocking
    {
        public OpenRoundRobinTournament()
        {
            Type = TournamentType.OpenRoundRobin;
            MatchLog = new OpenRoundRobinMatchLog();
        }

        public override void Start()
        {
            // TODO Add Open Round Robin specific start logic here
        }

        public override void End()
        {
            // TODO Add Open Round Robin specific end logic here
        }

        public override string GetFormattedType() => "Open Round Robin";
    }
}
