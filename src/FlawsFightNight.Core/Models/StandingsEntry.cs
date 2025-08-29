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
    }
}
