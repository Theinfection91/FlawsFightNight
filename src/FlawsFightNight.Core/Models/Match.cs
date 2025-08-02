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
        public Team? TeamA { get; set; }
        public Team? TeamB { get; set; }
        public bool IsCompleted { get; set; } = false;
        public bool IsByeMatch { get; set; } = false; // Indicates if this match is a bye match
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public Match(Team teamA, Team teamB)
        {
            TeamA = teamA;
            TeamB = teamB;
        }
        public override string ToString()
        {
            return $"{TeamA.Name ?? "Bye"} vs {TeamB.Name ?? "Bye"}";
        }
    }
}
