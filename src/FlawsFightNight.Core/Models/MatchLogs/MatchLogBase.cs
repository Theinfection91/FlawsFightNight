using FlawsFightNight.Core.Interfaces;
using FlawsFightNight.Core.Models.Tournaments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Models.MatchLogs
{
    public abstract class MatchLogBase : IMatchLog
    {
        public abstract void ClearLog();
        public abstract List<Match> GetAllActiveMatches(int currentRound = 0);
        public abstract List<PostMatch> GetAllPostMatches();
        public virtual (int pointsFor, int pointsAgainst) GetPointsForAndAgainst(string teamName)
        {
            int pointsFor = 0;
            int pointsAgainst = 0;
            foreach (var pm in GetAllPostMatches())
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
        public abstract bool ContainsMatchId(string matchId);
        public abstract Match? GetMatchById(string matchId);
        public abstract void ConvertMatchToPostMatch(TournamentBase tournament, Match match, string winningTeamName, int winningTeamScore, string losingTeamName, int losingTeamScore);
    }
}
