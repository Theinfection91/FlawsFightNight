using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Models.UT2004
{
    public class TaggedTournamentMatchEntry
    {
        public string MatchId { get; set; }
        public string TournamentId { get; set; }
        public string TournamentName { get; set; }
        public string StatLogId { get; set; }

        public TaggedTournamentMatchEntry()
        {

        }
    }
}
