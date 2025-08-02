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
        public Guid Id { get; set; } = Guid.NewGuid();
        public Team TeamA { get; set; }
        public Team TeamB { get; set; }
        public bool IsCompleted { get; set; } = false;
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime CompletedOn { get; set; }

        // Post-match details
        public Team? Winner { get; set; }
        public int WinnerScore { get; set; }
        public Team? Loser { get; set; }
        public int LoserScore { get; set; }

        public Match(Team teamA, Team teamB)
        {
            TeamA = teamA;
            TeamB = teamB;
        }
        public override string ToString()
        {
            return $"{TeamA.Name} vs {TeamB.Name}";
        }
    }
}
