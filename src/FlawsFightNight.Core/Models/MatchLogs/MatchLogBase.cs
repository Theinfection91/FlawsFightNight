using FlawsFightNight.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Models.MatchLogs
{
    public abstract class MatchLogBase : IMatchLog
    {
        public abstract List<Match> GetAllActiveMatches(int currentRound = 0);
        public abstract List<PostMatch> GetAllPostMatches();
        public virtual (int pointsFor, int pointsAgainst) GetPointsForAndAgainst(string teamName)
        => (0, 0);
    }
}
