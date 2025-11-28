using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Models.MatchLogs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Models.Tournaments
{
    public class DSRLadderTournament : Tournament
    {
        public static class RatingCalculator
        {
            public static int GetNewRating(int oldRating, int kFactor, double expectedScore, double actualScore)
            {
                return oldRating + (int)(kFactor * (actualScore - expectedScore));
            }

            public static double CalculateExpectedScore(int ratingA, int ratingB)
            {
                return 1.0 / (1.0 + Math.Pow(10, (ratingB - ratingA) / 400.0));
            }

            public static int GetKFactorForTeam(int rating)
            {
                if (rating < 2100)
                    return 32;
                else if (rating <= 2400)
                    return 24;
                else
                    return 16;
            }
        }

        public override TournamentType Type { get; protected set; } = TournamentType.DSRLadder;

        [JsonProperty(TypeNameHandling = TypeNameHandling.Auto)]
        public override MatchLog MatchLog { get; protected set; }

        [JsonConstructor]
        protected DSRLadderTournament() : base() { }

        public DSRLadderTournament(string id, string name, int teamSize) : base(id, name, teamSize)
        {
            MatchLog ??= new DSRLadderMatchLog();
        }

        public override bool CanStart()
        {
            // A DSR ladder tournament requires at least 3 teams to function properly
            return Teams.Count >= 3;
        }

        public override void Start()
        {
            IsRunning = true;

            // Reset team stats to zero
            foreach (var team in Teams)
            {
                team.Rating = 1200;
                team.ResetTeamToZero();
            }
        }

        public override bool CanEnd()
        {
            // If the tournament is running, it can be ended at any time
            return IsRunning;
        }

        public override void End()
        {
            IsRunning = false;
            // TODO Add DSR Ladder specific end logic here
        }

        public override string GetFormattedType() => "DSR Ladder";

        public override bool CanDelete() => !IsRunning;

        public override bool CanAcceptNewTeams() => true;

        public override void AdjustRanks()
        {
            // Sort teams by their current rating
            Teams.Sort((a, b) => a.Rating.CompareTo(b.Rating));

            // Reassign ranks sequentially starting from 1
            for (int i = 0; i < Teams.Count; i++)
            {
                Teams[i].Rank = i + 1;
            }
        }

        public void HandleTeamRatingChange(Team winner, Team loser, int winnerScore, int loserScore, out int winnerRatingChange, out int loserRatingChange)
        {
            // Store initial ratings
            int initialWinnerRating = winner.Rating;
            int initialLoserRating = loser.Rating;

            // --- Standard Elo calculation ---
            double expectedWinner = RatingCalculator.CalculateExpectedScore(winner.Rating, loser.Rating);
            double expectedLoser = RatingCalculator.CalculateExpectedScore(loser.Rating, winner.Rating);

            int kWinner = RatingCalculator.GetKFactorForTeam(winner.Rating);
            int kLoser = RatingCalculator.GetKFactorForTeam(loser.Rating);

            int baseWinnerChange = RatingCalculator.GetNewRating(winner.Rating, kWinner, expectedWinner, 1.0) - winner.Rating;
            int baseLoserChange = RatingCalculator.GetNewRating(loser.Rating, kLoser, expectedLoser, 0.0) - loser.Rating;

            // --- Margin of Victory bonus ---
            // For every 1 points difference in score, award or deduct an additional three points up to a maximum of 15 points
            int margin = Math.Abs(winnerScore - loserScore);
            int marginBonus = Math.Min(15, margin * 3);

            // --- Streak bonuses ---
            // For every win in the winner's streak beyond the first, award 4 points up to a maximum of 20 points
            int winStreakBonus = Math.Min(20, Math.Max(0, winner.WinStreak - 1) * 4);
            // For every loss in the loser's streak beyond the first, deduct 4 points up to a maximum of -20 points
            int lossStreakPenalty = Math.Max(-20, Math.Min(0, -(loser.LoseStreak - 1) * 4));

            // --- Apply rating changes ---
            // Winner gets full margin bonus, loser gets half the margin bonus deducted
            int finalWinnerChange = baseWinnerChange + marginBonus + winStreakBonus;
            int finalLoserChange = baseLoserChange + lossStreakPenalty - (marginBonus / 2);

            winner.Rating += finalWinnerChange;
            loser.Rating += finalLoserChange;

            // Clamp ratings from dropping below 100 (optional)
            //winner.Rating = Math.Max(100, winner.Rating);
            //loser.Rating = Math.Max(100, loser.Rating);

            // Output rating changes for embed and logging
            winnerRatingChange = winner.Rating - initialWinnerRating;
            loserRatingChange = loser.Rating - initialLoserRating;
        }

    }
}
