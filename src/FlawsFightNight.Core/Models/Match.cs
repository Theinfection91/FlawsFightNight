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
        public string Id { get; set; }
        public string TeamA { get; set; }
        public string TeamB { get; set; }
        public bool IsByeMatch { get; set; } = false;
        public int RoundNumber { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        // Ladder Specific Info
        public Challenge? Challenge { get; set; } = null;

        public Match(string teamA, string teamB)
        {
            TeamA = teamA;
            TeamB = teamB;
        }

        public string GetCorrectByeNameForByeMatch()
        {
            if (TeamA.Equals("BYE", StringComparison.OrdinalIgnoreCase))
            {
                return TeamB;
            }
            if (TeamB.Equals("BYE", StringComparison.OrdinalIgnoreCase))
            {
                return TeamA;
            }
            return "Error";
        }

        public string GetCorrectPlayerNameForByeMatch()
        {
            if (TeamA.Equals("BYE", StringComparison.OrdinalIgnoreCase))
            {
                return TeamB;
            }
            if (TeamB.Equals("BYE", StringComparison.OrdinalIgnoreCase))
            {
                return TeamA;
            }
            return "Error";
        }
    }
}
