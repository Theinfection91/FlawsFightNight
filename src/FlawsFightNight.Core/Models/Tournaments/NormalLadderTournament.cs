using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Models.MatchLogs;

namespace FlawsFightNight.Core.Models.Tournaments
{
    public class NormalLadderTournament : TournamentBase // TODO IRankSystem
    {
        public NormalLadderTournament()
        {
            Type = TournamentType.NormalLadder;
            MatchLog = new NormalLadderMatchLog();
        }

        public override void Start()
        {
            IsRunning = true;
            // TODO Add Ladder specific start logic here
        }

        public override void End() => IsRunning = false;

        public override string GetFormattedType() => "Normal Ladder";
    }
}
