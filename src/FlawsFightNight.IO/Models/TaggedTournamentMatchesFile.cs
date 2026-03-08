using FlawsFightNight.Core.Models.UT2004;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.IO.Models
{
    public class TaggedTournamentMatchesFile
    {
        public List<TaggedTournamentMatchEntry> TaggedTournamentMatchesEntries { get; set; } = new();

        public TaggedTournamentMatchesFile() { }
    }
}
