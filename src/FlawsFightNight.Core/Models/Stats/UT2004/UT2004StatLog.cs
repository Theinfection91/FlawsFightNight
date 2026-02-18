using FlawsFightNight.Core.Enums.UT2004;
using System;
using System.Collections.Generic;

namespace FlawsFightNight.Core.Models.Stats.UT2004
{
    public class UT2004StatLog : StatLog
    {
        public string? FileName { get; set; }
        public DateTime MatchDate { get; set; }
        public UT2004GameMode GameMode { get; set; }
        public string GameModeName => GetCorrectGameModeName();
        public List<List<UTPlayerMatchStats>> Players { get; set; } = new();
        
        public UT2004StatLog() { }

        public string GetCorrectGameModeName()
        {
            switch (GameMode)
            {
                case UT2004GameMode.iCTF:
                    return "iCTF";
                case UT2004GameMode.TAM:
                    return "TAM";
                case UT2004GameMode.iBR:
                    return "iBR";
                default:
                    return GameModeName ?? "Unknown";
            }
        }
    }
}
