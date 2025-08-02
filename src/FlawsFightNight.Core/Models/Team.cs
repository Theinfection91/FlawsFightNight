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
        public int Size { get; set; }
        public string TournamentName { get; set; }
        public string TournamentTeamSize { get; set; }
        public int Rank { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public List<Member> Members { get; set; }

        // Streaks
        public int WinStreak { get; set; }
        public int LoseStreak { get; set; }

        // Tournament Specific Fields
        #region Ladder
        public bool IsChallengeable { get; set; }
        #endregion

        #region Round Robin
        public int TotalScore { get; set; }
        #endregion
    }
}
