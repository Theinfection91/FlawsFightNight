using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Models.Stats.UT2004
{
    /// <summary>
    /// UTStatsDB-style ELO variant ratings for UT2004 game modes.
    /// Everyone starts at 0 and gains/loses points based on performance vs opponents.
    /// </summary>
    public class UT2004EloRatings
    {
        // Current ratings (starts at 0, not 1500 like traditional ELO)
        public double CaptureTheFlag { get; set; } = 0.0;
        public double TAM { get; set; } = 0.0;
        public double BombingRun { get; set; } = 0.0;

        // Last match rank changes (for displaying +/- change)
        public double CaptureTheFlagChange { get; set; } = 0.0;
        public double TAMChange { get; set; } = 0.0;
        public double BombingRunChange { get; set; } = 0.0;

        // Peak ratings (career best)
        public double CaptureTheFlagPeak { get; set; } = 0.0;
        public double TAMPeak { get; set; } = 0.0;
        public double BombingRunPeak { get; set; } = 0.0;

        // Dates of peak achievements
        public DateTime? CaptureTheFlagPeakDate { get; set; }
        public DateTime? TAMPeakDate { get; set; }
        public DateTime? BombingRunPeakDate { get; set; }

        public UT2004EloRatings() { }

        /// <summary>
        /// Updates peak rating if current rating exceeds previous peak.
        /// </summary>
        public void UpdatePeaks(DateTime matchDate)
        {
            if (CaptureTheFlag > CaptureTheFlagPeak)
            {
                CaptureTheFlagPeak = CaptureTheFlag;
                CaptureTheFlagPeakDate = matchDate;
            }

            if (TAM > TAMPeak)
            {
                TAMPeak = TAM;
                TAMPeakDate = matchDate;
            }

            if (BombingRun > BombingRunPeak)
            {
                BombingRunPeak = BombingRun;
                BombingRunPeakDate = matchDate;
            }
        }
    }
}
