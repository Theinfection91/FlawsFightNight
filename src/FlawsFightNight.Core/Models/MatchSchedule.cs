using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Models
{
    public class MatchSchedule
    {
        public Dictionary<int, List<Match>> MatchesToPlayByRound { get; set; } = [];

        public MatchSchedule() { }
    }
}
