using FlawsFightNight.Core.Attributes;
using FlawsFightNight.Core.Models.Tournaments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.IO.Models
{
    [SafeForSerialization]
    public class TournamentDataFile
    {
        public Tournament Tournament { get; set; }

        public TournamentDataFile() { }
    }
}
