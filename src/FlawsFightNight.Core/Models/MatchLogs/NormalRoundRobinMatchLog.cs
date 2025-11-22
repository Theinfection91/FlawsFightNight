using FlawsFightNight.Core.Models.Tournaments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Models.MatchLogs
{
    public class NormalRoundRobinMatchLog : MatchLogBase
    {
        public Dictionary<int, List<Match>> MatchesToPlayByRound { get; set; } = [];
        public Dictionary<int, List<PostMatch>> PostMatchesByRound { get; set; } = [];

        public NormalRoundRobinMatchLog() { }

        public override void ClearLog()
        {
            MatchesToPlayByRound.Clear();
            PostMatchesByRound.Clear();
        }

        public override List<Match> GetAllActiveMatches(int currentRound = 0)
        {
            if (currentRound > 0 && MatchesToPlayByRound.ContainsKey(currentRound))
            {
                return MatchesToPlayByRound[currentRound].Where(m => !m.IsByeMatch).ToList();
            }

            return new List<Match>();
        }

        public override List<PostMatch> GetAllPostMatches()
        {
            List<PostMatch> allPostMatches = new();
            foreach (var round in PostMatchesByRound.Values)
            {
                allPostMatches.AddRange(round);
            }
            return allPostMatches;
        }

        public bool IsRoundComplete(int roundNumber)
        {
            if (MatchesToPlayByRound.ContainsKey(roundNumber))
            {
                // Check if all non-bye matches have been reported in current round
                return MatchesToPlayByRound[roundNumber].Where(m => !m.IsByeMatch).ToList().Count == 0;
            }
            return false;
        }
    }
}
