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
        public int Rank { get; set; }
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

        public string GetFormattedStreakString()
        {
            if (WinStreak > 0 && LoseStreak == 0)
            {
                return $"W{WinStreak}";
            }
            if (LoseStreak > 0 && WinStreak == 0)
            {
                return $"L{LoseStreak}";
            }
            return "--";
        }

        public string GetCorrectStreakEmoji()
        {
            if (WinStreak > 0 && LoseStreak == 0)
            {
                return "📈";
            }
            if (LoseStreak > 0 && WinStreak == 0)
            {
                return "📉";
            }
            return "⌛";
        }

        public void ResetTeamToZero()
        {
            Wins = 0;
            Losses = 0;
            WinStreak = 0;
            LoseStreak = 0;
            TotalScore = 0;
        }

        public string GetMembersAsString()
        {
            return string.Join(", ", Members.Select(m => m.DisplayName));
        }
    }
}
