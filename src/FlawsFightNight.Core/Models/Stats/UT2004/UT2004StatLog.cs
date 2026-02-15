using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Models.Stats.UT2004
{
    public class UT2004StatLog : StatLog
    {
        public string? FileName { get; set; }
        public HashSet<List<UTPlayerMatchStats>> Players { get; set; } = new();
        
        public UT2004StatLog() { }
    }
}
