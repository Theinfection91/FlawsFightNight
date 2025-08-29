using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Models
{
    public class StandingsEntry
    {
        public int Rank { get; set; }
        public string TeamName { get; set; }
        public int Wins { get; set; }
        public int WinStreak { get; set; }
        public int Losses { get; set; }
        public int LoseStreak { get; set; }
        public int TotalScore { get; set; }
        public int PointsFor { get; set; }
        public int PointsAgainst { get; set; }

        public StandingsEntry(Team team)
        {
            TeamName = team.Name;
            Wins = team.Wins;
            WinStreak = team.WinStreak;
            Losses = team.Losses;
            LoseStreak = team.LoseStreak;
            TotalScore = team.TotalScore;
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

        public  string GetCorrectStreakEmoji()
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
    }
}
