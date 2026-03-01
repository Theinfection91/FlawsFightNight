using FlawsFightNight.Core.Attributes;
using FlawsFightNight.Core.Models.UT2004;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Data.Models
{
    [SafeForSerialization]
    public class StatLogMatchResultsFile
    {
        public UT2004StatLog StatLog { get; set; }
        public StatLogMatchResultsFile() { }
    }
}
