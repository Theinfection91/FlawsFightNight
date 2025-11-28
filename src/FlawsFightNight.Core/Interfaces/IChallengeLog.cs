using FlawsFightNight.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Interfaces
{
    public interface IChallengeLog
    {
        bool HasPendingChallenge(Team team, out Match challengeMatch);
        bool IsRankCorrectForTeam(Team team);
        Match? GetChallengeMatch(Team team);
        void RunChallengeRankCorrection(List<Team> challengeTeams);
    }
}
