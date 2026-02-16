using System;
using System.Collections.Generic;

namespace FlawsFightNight.Core.Models.Stats.UT2004
{
    public class UT2004StatLog : StatLog
    {
        public string? FileName { get; set; }
        public DateTime MatchDate { get; set; }
        public List<List<UTPlayerMatchStats>> Players { get; set; } = new();
        
        public UT2004StatLog() { }
    }
}
