using FlawsFightNight.Core.Interfaces.UT2004;
using FlawsFightNight.Core.Models.UT2004;
using System;

namespace FlawsFightNight.Core.Helpers.UT2004
{
    public class TAMScoreWeighter : IUTGameModeWeight
    {
        public double RoundsWonWeight { get; set; } = 6.0;
        public double RoundEndingKillWeight { get; set; } = 3.0;
        public double DamagePerRoundWeight { get; set; } = 0.01;
        public double RoundParticipationWeight { get; set; } = 0.2;
        public double FriendlyFirePenaltyWeight { get; set; } = -0.02;
        public double DamageTakenPenaltyWeight { get; set; } = -0.005;
        public double TeamProtectFragWeight { get; set; } = 1.0;
        public double CriticalFragWeight { get; set; } = 1.5;
        public double HeadshotWeight { get; set; } = 0.5;

        public TAMScoreWeighter() { }

        public double ApplyObjectiveWeights(UTPlayerMatchStats player, double baseScore, UTPlayerMatchStats? opponent = null)
        {
            double add = 0.0;

            // Primary TAM outcomes
            add += player.RoundsWon * RoundsWonWeight;
            add += player.RoundEndingKills * RoundEndingKillWeight;

            // Damage contribution (normalized per round)
            if (player.RoundsPlayed > 0)
            {
                double dmgPerRound = (double)player.TotalDamageDealt / player.RoundsPlayed;
                add += dmgPerRound * DamagePerRoundWeight;

                // penalize damage taken per round (encourages efficient damage)
                double takenPerRound = (double)player.TotalDamageTaken / player.RoundsPlayed;
                add += takenPerRound * DamageTakenPenaltyWeight;
            }

            // Participation reward (small)
            add += player.RoundsPlayed * RoundParticipationWeight;

            // Penalize friendly fire (absolute damage points)
            add += player.FriendlyFireDamage * FriendlyFirePenaltyWeight;

            // Other TAM-specific objective/assist stats
            add += player.TeamProtectFrags * TeamProtectFragWeight;
            add += player.CriticalFrags * CriticalFragWeight;

            // Headshots contribution (new)
            add += player.Headshots * HeadshotWeight;

            return add;
        }
    }
}
