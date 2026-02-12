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
        bool IsTeamChallenger(Team challengerTeam);
        bool IsRankCorrectForTeam(Team team);
        Match? GetChallengeMatch(Team team);
        public void RunChallengeRankCorrection(List<Team> challengeTeams)
        {
            foreach (var team in challengeTeams)
            {
                // Check if the team's rank is correct
                if (!IsRankCorrectForTeam(team))
                {
                    var challengeMatch = GetChallengeMatch(team);
                    if (challengeMatch != null && challengeMatch.Challenge != null)
                    {
                        // Update the rank in the challenge
                        if (challengeMatch.Challenge.Challenger.Equals(team.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine($"Correcting rank for challenger team '{team.Name}' from {challengeMatch.Challenge.ChallengerRank} to {team.Rank}");
                            challengeMatch.Challenge.ChallengerRank = team.Rank;
                        }
                        else if (challengeMatch.Challenge.Challenged.Equals(team.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine($"Correcting rank for challenged team '{team.Name}' from {challengeMatch.Challenge.ChallengedRank} to {team.Rank}");
                            challengeMatch.Challenge.ChallengedRank = team.Rank;
                        }
                    }
                }
            }
        }
    }
}
