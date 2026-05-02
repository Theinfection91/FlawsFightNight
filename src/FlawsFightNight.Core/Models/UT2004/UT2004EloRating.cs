using FlawsFightNight.Core.Attributes;
using System;

namespace FlawsFightNight.Core.Models.UT2004
{
    /// <summary>
    /// UTStatsDB ELO rating holder.
    /// Matches UTStatsDB design: ratings start at 0 and never go negative.
    /// </summary>
    [SafeForSerialization]
    public class UT2004EloRating
    {
        public double Rating { get; set; } = 0.0;
        public double Change { get; set; } = 0.0;
        public double Peak { get; set; } = 0.0;
        public DateTime? PeakDate { get; set; } = null;

        public UT2004EloRating() { }

        public void UpdateRating(double newRating, double change)
        {
            Rating = Math.Max(0.0, newRating);
            Change = change;
        }

        public void UpdatePeak(DateTime matchDate)
        {
            if (Rating > Peak)
            {
                Peak = Rating;
                PeakDate = matchDate;
            }
        }
    }
}