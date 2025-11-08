using FlawsFightNight.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Models
{
    public class MatchLog : IMatchLog
    {
        // Normal Round Robin Properties
        public Dictionary<int, List<Match>> MatchesToPlayByRound { get; set; } = [];
        public Dictionary<int, List<PostMatch>> PostMatchesByRound { get; set; } = [];

        // Open Round Robin Properties
        public List<Match> OpenRoundRobinMatchesToPlay { get; set; } = [];
        public List<PostMatch> OpenRoundRobinPostMatches { get; set; } = [];

        // Ladder Properties
        public List<Match> LadderMatchesToPlay { get; set; } = [];
        public List<PostMatch> LadderPostMatches { get; set; } = [];

        public MatchLog() { }

        public (int, int) GetPointsForAndAgainst(string teamName)
        {
            // Normal Round Robin
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
            // Open Round Robin
            foreach (var pm in OpenRoundRobinPostMatches)
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
            return (pointsFor, pointsAgainst);
        }

        public List<Match> GetAllActiveMatches(int currentRound = 0)
        {
            var allMatches = new List<Match>();
            // Normal Round Robin Matches (Only grab matches in current round)
            if (currentRound > 0 && MatchesToPlayByRound.ContainsKey(currentRound))
            {
                // Do not add bye matches
                allMatches.AddRange(MatchesToPlayByRound[currentRound].Where(m => !m.IsByeMatch));
            }
            // Open Round Robin Matches
            allMatches.AddRange(OpenRoundRobinMatchesToPlay);

            // Ladder Matches
            allMatches.AddRange(LadderMatchesToPlay);

            // TODO Add Elimination Matches when that is implemented
            return allMatches;
        }

        public List<PostMatch> GetAllPostMatches()
        {
            var allPostMatches = new List<PostMatch>();
            // Normal Round Robin Post Matches
            foreach (var round in PostMatchesByRound.Values)
            {
                allPostMatches.AddRange(round);
            }
            // Open Round Robin Post Matches
            allPostMatches.AddRange(OpenRoundRobinPostMatches);
            // Ladder Post Matches
            allPostMatches.AddRange(LadderPostMatches);
            return allPostMatches;
        }

        public List<PostMatch> GetEditablePostMatches()
        {
            var allPostMatches = new List<PostMatch>();
            // Normal Round Robin Post Matches
            foreach (var round in PostMatchesByRound.Values)
            {
                allPostMatches.AddRange(round);
            }
            // Open Round Robin Post Matches
            allPostMatches.AddRange(OpenRoundRobinPostMatches);
            return allPostMatches;
        }
    }
}
