using System;

namespace FlawsFightNight.Core.Models.Stats.UT2004
{
    /// <summary>
    /// UTStatsDB ELO rating holder.
    /// Matches UTStatsDB design: ratings start at 0 and never go negative.
    /// </summary>
    public class UT2004EloRating
    {
        // Start at 0 per UTStatsDB design (not 1500).
        public double Rating { get; set; } = 0.0;

        // Last match change (positive or negative)
        public double Change { get; set; } = 0.0;

        // Career peak value and date
        public double Peak { get; set; } = 0.0;
        public DateTime? PeakDate { get; set; } = null;

        public UT2004EloRating() { }

        public void UpdateRating(double newRating, double change)
        {
            Rating = Math.Max(0.0, newRating);
            Change = change;
        }

        // Call this after the configured minimum matches for the mode are met
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