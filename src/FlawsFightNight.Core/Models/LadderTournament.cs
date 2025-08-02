using FlawsFightNight.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Models
{
    public class LadderTournament : Tournament
    {
        public LadderTournament(string name, string? description = null) 
            : base(name, description)
        {
            Type = TournamentType.Ladder;
        }
    }
}
