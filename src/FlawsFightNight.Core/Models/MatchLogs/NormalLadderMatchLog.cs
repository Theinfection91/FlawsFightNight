using FlawsFightNight.Core.Models.Tournaments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Models.MatchLogs
{
    public class NormalLadderMatchLog : MatchLogBase
    {
        public List<Match> MatchesToPlay { get; set; } = [];
        public List<PostMatch> PostMatches { get; set; } = [];

        public NormalLadderMatchLog() { }

        public override void ClearLog()
        {
            MatchesToPlay.Clear();
            PostMatches.Clear();
        }

        public override List<Match> GetAllActiveMatches(int currentRound = 0) => MatchesToPlay;
        public override List<PostMatch> GetAllPostMatches() => PostMatches;
        public override bool ContainsMatchId(string matchId)
        {
            return MatchesToPlay.Any(m => m.Id.Equals(matchId, StringComparison.OrdinalIgnoreCase));
        }
        public override Match? GetMatchById(string matchId)
        {
            return MatchesToPlay.FirstOrDefault(m => m.Id.Equals(matchId, StringComparison.OrdinalIgnoreCase));
        }
        public override void ConvertMatchToPostMatch(TournamentBase tournament, Match match, string winningTeamName, int winningTeamScore, string losingTeamName, int losingTeamScore)
        {
            // TODO Normal Ladder Post Match Conversion Logic
        }
    }
}
