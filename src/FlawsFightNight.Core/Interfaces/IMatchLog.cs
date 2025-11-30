using FlawsFightNight.Core.Models;
using FlawsFightNight.Core.Models.Tournaments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Interfaces
{
    public interface IMatchLog
    {
        void ClearLog();
        List<Match> GetAllActiveMatches(int currentRound = 0);
        List<PostMatch> GetAllPostMatches();
        bool ContainsMatchId(string matchId);
        Match? GetMatchById(string matchId);
        void AddMatch(Match match);
        void RemoveMatch(Match match);
        (int pointsFor, int pointsAgainst) GetPointsForAndAgainst(string teamName);
        void ConvertMatchToPostMatch(Tournament tournament, Match match, string winningTeamName, int winningTeamScore, string losingTeamName, int losingTeamScore);
    }
}
