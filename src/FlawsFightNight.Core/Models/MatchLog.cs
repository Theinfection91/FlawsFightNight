using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Models
{
    public class MatchLog
    {
        public Dictionary<int, List<Match>> MatchesToPlayByRound { get; set; } = [];
        public Dictionary<int, List<PostMatch>> PostMatchesByRound { get; set; } = [];

        public MatchLog() { }

        public (int, int) GetPointsForAndPointsAgainstForTeam(string teamName)
        {
            int pointsFor = 0;
            int pointsAgainst = 0;
            foreach (var round in PostMatchesByRound.Values)
            {
                foreach (var pm in round)
                {
                    if (pm.WasByeMatch) continue;
                    if (pm.Winner == teamName)
                    {
                        pointsFor += pm.WinnerScore;
                        pointsAgainst += pm.LoserScore;
                    }
                    else if (pm.Loser == teamName)
                    {
                        pointsFor += pm.LoserScore;
                        pointsAgainst += pm.WinnerScore;
                    }
                }
            }
            return (pointsFor, pointsAgainst);
        }
    }
}
