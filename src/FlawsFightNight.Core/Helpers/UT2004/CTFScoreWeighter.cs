using FlawsFightNight.Core.Interfaces.UT2004;
using FlawsFightNight.Core.Models.UT2004;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Helpers.UT2004
{
    public class CTFScoreWeighter : IUTGameModeWeight
    {
        public double FlagCaptureWeight { get; set; } = 5.0;
        public double FlagReturnWeight { get; set; } = 1.0;
        public double FlagGrabWeight { get; set; } = 0.5;
        public double FlagDenialWeight { get; set; } = 1.0;
        public double AssistWeight { get; set; } = 1.0;
        public double FirstTouchWeight { get; set; } = 1.0;

        public CTFScoreWeighter() { }

        public double ApplyObjectiveWeights(UTPlayerMatchStats player, double baseScore, UTPlayerMatchStats? opponent = null)
        {
            double add = 0.0;
            add += player.FlagCaptures * FlagCaptureWeight;
            add += player.FlagReturns * FlagReturnWeight;
            add += player.FlagGrabs * FlagGrabWeight;
            add += player.FlagDenials * FlagDenialWeight;
            add += player.FlagCaptureAssists * AssistWeight;
            add += player.FlagCaptureFirstTouch * FirstTouchWeight;
            return add;
        }
    }
}
