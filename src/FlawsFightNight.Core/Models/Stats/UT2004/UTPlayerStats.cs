using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Models.Stats.UT2004
{
    public class UTPlayerStats
    {
        public string? Guid { get; set; }
        public string? LastKnownName { get; set; }
        public int Team { get; set; } // 0 = Red, 1 = Blue
        public int Placement { get; set; }
    }
}
