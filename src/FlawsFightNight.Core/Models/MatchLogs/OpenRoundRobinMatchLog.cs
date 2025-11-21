using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Models.MatchLogs
{
    public class OpenRoundRobinMatchLog : MatchLogBase
    {
        public List<Match> MatchesToPlay { get; set; } = [];
        public List<PostMatch> PostMatches { get; set; } = [];

        public OpenRoundRobinMatchLog() { }

        public override void ClearLog()
        {
            MatchesToPlay.Clear();
            PostMatches.Clear();
        }

        public override List<Match> GetAllActiveMatches(int currentRound = 0) => MatchesToPlay;
        public override List<PostMatch> GetAllPostMatches() => PostMatches;
    }
}
