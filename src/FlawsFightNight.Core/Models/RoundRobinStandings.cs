using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Models
{
    public class RoundRobinStandings
    {

        public List<StandingsEntry> Entries { get; set; } = [];

        public RoundRobinStandings() { }

        public void SortStandings()
        {
            Entries = Entries
                .OrderBy(e => e.Rank)
                .ThenByDescending(e => e.Wins)
                .ThenBy(e => e.Losses)
                .ThenByDescending(e => e.TotalScore)
                //.ThenBy(e => e.TeamName)
                .ToList();
        }
    }
}
