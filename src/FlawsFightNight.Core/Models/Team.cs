using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Models
{
    public class Team
    {
        // Basic Info
        public string Name { get; set; }
        public int? Rank { get; set; }
        public int Wins { get; set; } = 0;
        public int Losses { get; set; } = 0;
        public List<Member> Members { get; set; }

        // Streaks
        public int WinStreak { get; set; } = 0;
        public int LoseStreak { get; set; } = 0;

        // Tournament Specific Fields
        #region Ladder
        public bool IsChallengeable { get; set; } = true;
        #endregion

        #region Round Robin
        public int TotalScore { get; set; } = 0;
        #endregion

        public Team() { }

        public string GetFormattedChallengeStatus()
        {
            if (IsChallengeable)
            {
                return "Free";
            }
            else
            {
                return "Challenged";
            }
        }
    }
}
