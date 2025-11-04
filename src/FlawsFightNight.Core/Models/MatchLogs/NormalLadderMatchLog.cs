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

        public override List<Match> GetAllActiveMatches(int currentRound = 0) => MatchesToPlay;
        public override List<PostMatch> GetAllPostMatches() => PostMatches;
    }
}
