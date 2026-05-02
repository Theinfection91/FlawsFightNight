using FlawsFightNight.Core.Interfaces.UT2004;
using FlawsFightNight.Core.Models.UT2004;

namespace FlawsFightNight.Core.Helpers.UT2004
{
    public class BRScoreWeighter : IUTGameModeWeight
    {
        // Primary Objective Weights
        // Capture is the top contribution (greater than CTF flag capture=5.0 and TAM rounds won=6.0)
        public double BallCaptureWeight { get; set; } = 7.0;
        public double BallAssistWeight { get; set; } = 3.0;
        public double BallThrownFinalWeight { get; set; } = 4.0;
        
        // Ball Handling Weights
        // Picking up / securing the ball is useful but lesser than assists/throws
        public double BombPickupWeight { get; set; } = 0.75;
        public double BombDropWeight { get; set; } = -1.5;
        public double BombTakenWeight { get; set; } = 1.5;
        public double BombReturnedTimeoutWeight { get; set; } = 0.5;

        // Combat Objective Weights
        // Support combat actions that enable objectives — weighted modestly (TAM uses 1.0 and 1.5)
        public double TeamProtectFragWeight { get; set; } = 1.0;
        public double CriticalFragWeight { get; set; } = 1.5;

        public BRScoreWeighter() { }

        public double ApplyObjectiveWeights(UTPlayerMatchStats player, double baseScore, UTPlayerMatchStats? opponent = null)
        {
            double add = 0.0;
            
            // Primary Objectives
            add += player.BallCaptures * BallCaptureWeight;
            add += player.BallScoreAssists * BallAssistWeight;
            add += player.BallThrownFinals * BallThrownFinalWeight;
            
            // Ball Handling
            add += player.BombPickups * BombPickupWeight;
            add += player.BombDrops * BombDropWeight;
            add += player.BombTaken * BombTakenWeight;
            add += player.BombReturnedTimeouts * BombReturnedTimeoutWeight;
            
            // Combat Objectives
            add += player.TeamProtectFrags * TeamProtectFragWeight;
            add += player.CriticalFrags * CriticalFragWeight;
            
            return add;
        }
    }
}
