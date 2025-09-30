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

        // Ladder Specific Info
        public Challenge? Challenge { get; set; } = null;

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

        public void UpdateResultsProcess(string winningTeamName, int winningTeamScore, int losingTeamScore)
        {
            // Check if winner stays the same
            if (!Winner.Equals(winningTeamName, StringComparison.OrdinalIgnoreCase))
            {
                // Swap winner and loser, update scores
                string previousWinner = Winner;
                string previousLoser = Loser;
                Winner = previousLoser;
                WinnerScore = winningTeamScore;
                Loser = previousWinner;
                LoserScore = losingTeamScore;
            }
            else
            {
                // Just update scores
                WinnerScore = winningTeamScore;
                LoserScore = losingTeamScore;
            }
        }
    }
}
