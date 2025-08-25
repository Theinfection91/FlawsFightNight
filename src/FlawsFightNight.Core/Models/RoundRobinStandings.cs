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
                .OrderByDescending(e => e.Wins)
                .ThenByDescending(e => e.TotalScore)
                .ThenBy(e => e.TeamName) // stable alphabetical fallback
                .ToList();
        }
    }
}
