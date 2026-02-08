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
        public int WinnerRatingChange { get; set; }
        public string Loser { get; set; }
        public int LoserScore { get; set; }
        public int LoserRatingChange { get; set; }
        public bool WasByeMatch { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime CompletedOn { get; set; } = DateTime.UtcNow;

        // Ladder Specific Info
        public Challenge? Challenge { get; set; }

        public PostMatch(string matchId, string winner, int winnerScore, string loser, int loserScore, DateTime createdOn, bool wasByeMatch = false, Challenge challenge = null)
        {
            Id = matchId;
            Winner = winner;
            WinnerScore = winnerScore;
            Loser = loser;
            LoserScore = loserScore;
            CreatedOn = createdOn;
            WasByeMatch = wasByeMatch;
            Challenge = challenge;
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

        #region Normal Ladder Specific
        public bool IsChallengerWinner()
        {
            if (Challenge != null)
                return Winner.Equals(Challenge.Challenger, StringComparison.OrdinalIgnoreCase);

            return false;
        }

        public string GetRankTransitionText()
        {
            if (IsChallengerWinner())
            {
                return $"⬆️ {Winner} becomes (#{Challenge.ChallengedRank}) - {Loser} drops down one.";
            }
            else
            {
                return $"⏸️ {Winner} remains (#{Challenge.ChallengedRank}) - No rank change for {Loser}";
            }
        }
        #endregion

        #region DSR Ladder Specific
        public string GetRatingChangeText()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"{Winner} Rating Change: {(WinnerRatingChange >= 0 ? "+" : "")}{WinnerRatingChange}");
            sb.AppendLine($"{Loser} Rating Change: {(LoserRatingChange >= 0 ? "+" : "")}{LoserRatingChange}");
            return sb.ToString();
        }
        #endregion
    }
}
