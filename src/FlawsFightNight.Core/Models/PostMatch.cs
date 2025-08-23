using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Models
{
    public class PostMatch
    {
        public string Id { get; set; }
        public string Winner { get; set; }
        public int WinnerScore { get; set; }
        public string Loser { get; set; }
        public int LoserScore { get; set; }
        public bool WasByeMatch { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime CompletedOn { get; set; } = DateTime.UtcNow;

        public PostMatch(string matchId, string winner, int winnerScore, string loser, int loserScore, DateTime createdOn, bool wasByeMatch = false)
        {
            Id = matchId;
            Winner = winner;
            WinnerScore = winnerScore;
            Loser = loser;
            LoserScore = loserScore;
            CreatedOn = createdOn;
            WasByeMatch = wasByeMatch;
        }
    }
}
