using FlawsFightNight.Core.Attributes;
using FlawsFightNight.Core.Models.UT2004;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.IO.Models
{
    [SafeForSerialization]
    public class UT2004PlayerProfileFile
    {
        public UT2004PlayerProfile PlayerProfile { get; set; }
        public UT2004PlayerProfileFile() { }
    }
}
