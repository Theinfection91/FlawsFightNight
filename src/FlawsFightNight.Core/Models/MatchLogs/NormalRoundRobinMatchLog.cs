using FlawsFightNight.Core.Interfaces;
using FlawsFightNight.Core.Models.Tournaments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Models.MatchLogs
{
    public class NormalRoundRobinMatchLog : MatchLogBase
    {
        public Dictionary<int, List<Match>> MatchesToPlayByRound { get; set; } = [];
        public Dictionary<int, List<PostMatch>> PostMatchesByRound { get; set; } = [];

        public NormalRoundRobinMatchLog()
        {

        }

        public override void ClearLog()
        {
            MatchesToPlayByRound.Clear();
            PostMatchesByRound.Clear();
        }

        public bool DoesRoundContainMatchId(int roundNumber, string matchId)
        {
            if (MatchesToPlayByRound.ContainsKey(roundNumber))
            {
                return MatchesToPlayByRound[roundNumber].Any(m => m.Id.Equals(matchId, StringComparison.OrdinalIgnoreCase));
            }
            return false;
        }

        public override List<Match> GetAllActiveMatches(int currentRound = 0)
        {
            foreach (var round in MatchesToPlayByRound.Values)
            {
                foreach (var match in round)
                {
                    return MatchesToPlayByRound.Values.SelectMany(r => r).Where(m => !m.IsByeMatch).ToList();
                }
            }
            return [];
        }

        public override List<PostMatch> GetAllPostMatches()
        {
            List<PostMatch> allPostMatches = new();
            foreach (var round in PostMatchesByRound.Values)
            {
                allPostMatches.AddRange(round);
            }
            return allPostMatches;
        }

        public override bool ContainsMatchId(string matchId)
        {
            //foreach (var round in MatchesToPlayByRound.Values)
            //{
            //    if (round.Any(m => m.Id.Equals(matchId, StringComparison.OrdinalIgnoreCase)))
            //    {
            //        return true;
            //    }
            //}
            //foreach (var round in PostMatchesByRound.Values)
            //{
            //    if (round.Any(pm => pm.Id.Equals(matchId, StringComparison.OrdinalIgnoreCase)))
            //    {
            //        return true;
            //    }
            //}
            foreach (var match in GetAllActiveMatches())
            {
                if (match.Id.Equals(matchId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            foreach (var postMatch in GetAllPostMatches())
            {
                if (postMatch.Id.Equals(matchId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public override Match? GetMatchById(string matchId)
        {
            foreach (var round in MatchesToPlayByRound.Values)
            {
                var match = round.FirstOrDefault(m => m.Id.Equals(matchId, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    return match;
                }
            }
            return null;
        }

        public override void AddMatch(Match match)
        {
            if (!MatchesToPlayByRound.ContainsKey(match.RoundNumber))
            {
                MatchesToPlayByRound[match.RoundNumber] = new List<Match>();
            }
            MatchesToPlayByRound[match.RoundNumber].Add(match);
        }

        public override void RemoveMatch(Match match)
        {
            foreach (var roundNumber in MatchesToPlayByRound.Keys.ToList())
            {
                if (MatchesToPlayByRound[roundNumber].Remove(match))
                {
                    // If no more matches left in this round, remove the round entry
                    if (MatchesToPlayByRound[roundNumber].Count == 0)
                    {
                        MatchesToPlayByRound.Remove(roundNumber);
                    }
                    break;
                }
            }
        }

        public bool IsRoundComplete(int roundNumber)
        {
            if (MatchesToPlayByRound.ContainsKey(roundNumber))
            {
                // Check if all non-bye matches have been reported in current round
                return MatchesToPlayByRound[roundNumber].Where(m => !m.IsByeMatch).ToList().Count == 0;
            }
            return false;
        }

        public override void ConvertMatchToPostMatch(TournamentBase tournament, Match match, string winningTeamName, int winningTeamScore, string losingTeamName, int losingTeamScore)
        {
            if (tournament is IRoundBased rbTournament)
            {
                // Create the PostMatch
                PostMatch postMatch = new(match.Id, winningTeamName, winningTeamScore, losingTeamName, losingTeamScore, DateTime.UtcNow, match.IsByeMatch);

                // Check if key for current round exists, if not create it
                if (!PostMatchesByRound.ContainsKey(rbTournament.CurrentRound))
                {
                    PostMatchesByRound[rbTournament.CurrentRound] = new List<PostMatch>();
                }

                // Add the PostMatch to the round's list
                PostMatchesByRound[rbTournament.CurrentRound].Add(postMatch);

                // Remove the match from MatchesToPlay
                if (MatchesToPlayByRound.ContainsKey(rbTournament.CurrentRound))
                {
                    MatchesToPlayByRound[rbTournament.CurrentRound].Remove(match);

                    // If no more matches left in this round, remove the round entry
                    if (MatchesToPlayByRound[rbTournament.CurrentRound].Count == 0)
                    {
                        MatchesToPlayByRound.Remove(rbTournament.CurrentRound);

                        // Set round as complete for the tournament
                        rbTournament.IsRoundComplete = true;
                    }

                    // If only a bye match was left, mark round as complete. Bye match will be cleared on advance round logic.
                    if (MatchesToPlayByRound[rbTournament.CurrentRound].Count == 1 && rbTournament.DoesRoundContainByeMatch())
                    {
                        rbTournament.IsRoundComplete = true;
                    }
                }
            }
        }

        public void ConvertByeMatch(int roundNumber)
        {
            if (MatchesToPlayByRound.ContainsKey(roundNumber))
            {
                foreach (var match in MatchesToPlayByRound[roundNumber])
                {
                    if (match.IsByeMatch)
                    {
                        // Create a PostMatch for the bye match
                        PostMatch postMatch = new(match.Id, match.GetCorrectByeNameForByeMatch(), 0, "BYE", 0, DateTime.UtcNow, true);

                        // If no PostMatches list for this round, create it
                        if (!PostMatchesByRound.ContainsKey(roundNumber))
                        {
                            PostMatchesByRound[roundNumber] = new List<PostMatch>();
                        }

                        // Add the PostMatch to the round's list
                        PostMatchesByRound[roundNumber].Add(postMatch);

                        // Remove the match from MatchesToPlay
                        MatchesToPlayByRound[roundNumber].Remove(match);

                        // If no more matches left in this round, remove the round entry
                        if (MatchesToPlayByRound[roundNumber].Count == 0)
                        {
                            MatchesToPlayByRound.Remove(roundNumber);
                        }
                    }
                }
            }
        }
    }
}
