using FlawsFightNight.Core.Interfaces.UT2004;
using FlawsFightNight.Core.Models.UT2004;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Helpers.UT2004
{
    public class BRScoreWeighter : IUTGameModeWeight
    {
        public double BallCaptureWeight { get; set; } = 5.0;
        public double BallAssistWeight { get; set; } = 1.0;
        public double BallThrownFinalWeight { get; set; } = 2.0;
        public double BombPickupWeight { get; set; } = 0.5;
        public double BombDropWeight { get; set; } = -0.5;

        public BRScoreWeighter() { }

        public double ApplyObjectiveWeights(UTPlayerMatchStats player, double baseScore, UTPlayerMatchStats? opponent = null)
        {
            double add = 0.0;
            add += player.BallCaptures * BallCaptureWeight;
            add += player.BallScoreAssists * BallAssistWeight;
            add += player.BallThrownFinals * BallThrownFinalWeight;
            add += player.BombPickups * BombPickupWeight;
            add += player.BombDrops * BombDropWeight;
            return add;
        }
    }
}
