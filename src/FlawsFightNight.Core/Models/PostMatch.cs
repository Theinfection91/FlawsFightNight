using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Models
{
    public class PostMatch
    {
        public Team Winner { get; set; }
        public int WinnerScore { get; set; }
        public Team Loser { get; set; }
        public int LoserScore { get; set; }
        public bool WasByeMatch { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime CompletedOn { get; set; } = DateTime.UtcNow;

        public PostMatch(Team winner, int winnerScore, Team loser, int loserScore, DateTime createdOn)
        {
            Winner = winner;
            WinnerScore = winnerScore;
            Loser = loser;
            LoserScore = loserScore;
            CreatedOn = createdOn;
        }
    }
}
