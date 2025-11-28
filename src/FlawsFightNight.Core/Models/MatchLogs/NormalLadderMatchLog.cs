using FlawsFightNight.Core.Interfaces;
using FlawsFightNight.Core.Models.Tournaments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Models.MatchLogs
{
    public class NormalLadderMatchLog : MatchLog, IChallengeLog
    {
        public List<Match> MatchesToPlay { get; set; } = [];
        public List<PostMatch> PostMatches { get; set; } = [];

        public NormalLadderMatchLog() { }

        #region Overrides
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

        public override void AddMatch(Match match)
        {
            MatchesToPlay.Add(match);
        }

        public override void RemoveMatch(Match match)
        {
            MatchesToPlay.Remove(match);
        }

        public override void ConvertMatchToPostMatch(Tournament tournament, Match match, string winningTeamName, int winningTeamScore, string losingTeamName, int losingTeamScore)
        {
            // Ensure the match exists in active matches
            if (!GetAllActiveMatches().Contains(match))
            {
                return;
            }

            // Create PostMatch
            PostMatch postMatch = new(match.Id, winningTeamName, winningTeamScore, losingTeamName, losingTeamScore, match.CreatedOn, match.IsByeMatch, match.Challenge);

            // Add to PostMatches
            PostMatches.Add(postMatch);

            // Remove from active Matches
            RemoveMatch(match);
        }
        #endregion

        #region IChallengeLog
        public bool HasPendingChallenge(Team team, out Match challengeMatch)
        {
            foreach (var match in MatchesToPlay)
            {
                // Check by match, not challenge
                if (match.TeamA.Equals(team.Name, StringComparison.OrdinalIgnoreCase) ||
                    match.TeamB.Equals(team.Name, StringComparison.OrdinalIgnoreCase))
                {
                    challengeMatch = match;
                    return true;
                }
            }
            challengeMatch = null;
            return false;
        }

        public bool IsRankCorrectForTeam(Team team)
        {
            foreach (var match in MatchesToPlay)
            {
                if (match.Challenge.Challenger.Equals(team.Name, StringComparison.OrdinalIgnoreCase) || match.Challenge.Challenged.Equals(team.Name, StringComparison.OrdinalIgnoreCase))
                {
                    if (match.Challenge.ChallengerRank == team.Rank || match.Challenge.ChallengedRank == team.Rank)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public Match? GetChallengeMatch(Team team)
        {
            foreach (var match in MatchesToPlay)
            {
                if (match.TeamA.Equals(team.Name, StringComparison.OrdinalIgnoreCase) ||
                    match.TeamB.Equals(team.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return match;
                }
            }
            return null;
        }

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
                            challengeMatch.Challenge.ChallengerRank = team.Rank;
                        }
                        else if (challengeMatch.Challenge.Challenged.Equals(team.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            challengeMatch.Challenge.ChallengedRank = team.Rank;
                        }
                    }
                }
            }
        }
        #endregion
    }
}
