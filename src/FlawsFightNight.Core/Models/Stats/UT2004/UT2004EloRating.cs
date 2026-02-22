using System;

namespace FlawsFightNight.Core.Models.Stats.UT2004
{
    /// <summary>
    /// Represents UTStatsDB-style ELO variant rating for a single game mode.
    /// Everyone starts at 0 and gains/loses points based on performance vs opponents.
    /// </summary>
    public class UT2004EloRating
    {
        // Current rating (starts at 0, not 1500 like traditional ELO)
        public double Rating { get; set; } = 0.0;

        // Last match rank change (for displaying +/- change)
        public double Change { get; set; } = 0.0;

        // Peak rating (career best)
        public double Peak { get; set; } = 0.0;

        // Date of peak achievement
        public DateTime? PeakDate { get; set; }

        public UT2004EloRating() { }

        /// <summary>
        /// Updates the rating and change values.
        /// </summary>
        public void UpdateRating(double newRating, double change)
        {
            Rating = Math.Max(0, newRating); // Ensure rating never goes negative
            Change = change;
        }

        /// <summary>
        /// Updates peak rating if current rating exceeds previous peak.
        /// </summary>
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