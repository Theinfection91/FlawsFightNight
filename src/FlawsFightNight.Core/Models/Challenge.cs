using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Models
{
    public class Challenge
    {
        public string Challenger { get; set; }
        public int ChallengerRank { get; set; }
        public string Challenged { get; set; }
        public int ChallengedRank { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public Challenge(string challenger, int challengerRank, string challenged, int challengedRank)
        {
            Challenger = challenger;
            ChallengerRank = challengerRank;
            Challenged = challenged;
            ChallengedRank = challengedRank;
        }

        public override bool Equals(object? obj)
        {
            // Check if object is a Challenge
            if (obj is Challenge other)
            {
                return this.Challenger == other.Challenger && this.Challenged == other.Challenged;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Challenger, Challenged);
        }
    }
}
