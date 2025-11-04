using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Models.MatchLogs;

namespace FlawsFightNight.Core.Models.Tournaments
{
    public class NormalRoundRobinTournament : TournamentBase // TODO IRoundBased, ITieBreaker and ITeamLocking
    {
        public NormalRoundRobinTournament()
        {
            Type = TournamentType.NormalRoundRobin;
            MatchLog = new NormalRoundRobinMatchLog();
        }

        public override void Start()
        {
            // TODO Transfer Normal Round Robin specific Start logic here
        }

        public override void End()
        {
            // TODO Transfer Normal Round Robin specific End logic here
        }

        public override string GetFormattedType() => "Normal Round Robin";
    }
}
