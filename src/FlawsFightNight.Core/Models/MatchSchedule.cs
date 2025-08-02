using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Models
{
    public class MatchSchedule
    {
        public Dictionary<int, Match> MatchesToPlay { get; set; } = [];

        public MatchSchedule() { }
    }
}
