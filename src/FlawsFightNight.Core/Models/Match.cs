using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Models
{
    public class Match
    {
        // Basic Info
        public string TeamA { get; set; }
        public string TeamB { get; set; }
        public bool IsByeMatch { get; set; } = false;
        public int RoundNumber { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public Match(string teamA, string teamB)
        {
            TeamA = teamA;
            TeamB = teamB;
        }
    }
}
