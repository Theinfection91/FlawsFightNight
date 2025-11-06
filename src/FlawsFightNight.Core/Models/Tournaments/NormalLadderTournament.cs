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

        public override bool IsReadyToStart()
        {
            // A ladder tournament requires at least 3 teams to function properly
            return Teams.Count >= 3;
        }

        public override void Start()
        {
            IsRunning = true;
            // TODO Add Ladder specific start logic here
        }

        public override bool IsReadyToEnd()
        {
            // A ladder tournament can be ended at any time
            return true;
        }

        public override void End()
        {
            IsRunning = false;
            // TODO Add Ladder specific end logic here
        }

        public override string GetFormattedType() => "Normal Ladder";
    }
}
