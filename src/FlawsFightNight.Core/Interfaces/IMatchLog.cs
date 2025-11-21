using FlawsFightNight.Core.Models;
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
        (int pointsFor, int pointsAgainst) GetPointsForAndAgainst(string teamName);
    }
}
