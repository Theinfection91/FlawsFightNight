using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Managers
{
    public class MatchManager : BaseDataDriven
    {
        class UnorderedPairComparer : IEqualityComparer<(string A, string B)>
        {
            // Super advanced, I got help with this one.
            public bool Equals((string A, string B) x, (string A, string B) y) =>
                (string.Equals(x.A, y.A, StringComparison.OrdinalIgnoreCase) && string.Equals(x.B, y.B, StringComparison.OrdinalIgnoreCase)) ||
                (string.Equals(x.A, y.B, StringComparison.OrdinalIgnoreCase) && string.Equals(x.B, y.A, StringComparison.OrdinalIgnoreCase));

            public int GetHashCode((string A, string B) obj)
            {
                var a = obj.A.ToLowerInvariant();
                var b = obj.B.ToLowerInvariant();
                var first = string.CompareOrdinal(a, b) <= 0 ? a : b;
                var second = string.CompareOrdinal(a, b) <= 0 ? b : a;
                return HashCode.Combine(first, second);
            }
        }

        public MatchManager(DataManager dataManager) : base("MatchManager", dataManager)
        {

        }

        /// <summary>
        /// Checks if any match has been made for the specified team across all tournaments.
        /// </summary>
        /// <param name="teamName">The name of the team to check for.</param>
        /// <returns>True if a match exists for the team; otherwise, false.</returns>
        public bool IsMatchMadeForTeam(string teamName)
        {
            List<Tournament> tournaments = _dataManager.TournamentsDatabaseFile.Tournaments;
            foreach (var tournament in tournaments)
            {
                if (tournament.MatchLog.MatchesToPlayByRound.Any(round => round.Value.Any(match => match.TeamA.Equals(teamName, StringComparison.OrdinalIgnoreCase) || match.TeamB.Equals(teamName, StringComparison.OrdinalIgnoreCase))))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if any match has been made for the specified team within a given tournament.
        /// </summary>
        /// <param name="tournament">The tournament to check in.</param>
        /// <param name="teamName">The name of the team to check for.</param>
        /// <returns>True if a match exists for the team in the tournament; otherwise, false.</returns>
        public bool IsMatchMadeForTeam(Tournament tournament, string teamName)
        {
            if (tournament.MatchLog.MatchesToPlayByRound.Any(round => round.Value.Any(match => match.TeamA.Equals(teamName, StringComparison.OrdinalIgnoreCase) || match.TeamB.Equals(teamName, StringComparison.OrdinalIgnoreCase))))
            {
                return true;
            }
            return false;
        }

        public Match GetMatchByTeamName(string teamName)
        {
            List<Tournament> tournaments = _dataManager.TournamentsDatabaseFile.Tournaments;
            foreach (var tournament in tournaments)
            {
                foreach (var round in tournament.MatchLog.MatchesToPlayByRound.Values)
                {
                    foreach (var match in round)
                    {
                        if ((match.TeamA != null && match.TeamA.Equals(teamName, StringComparison.OrdinalIgnoreCase)) ||
                            (match.TeamB != null && match.TeamB.Equals(teamName, StringComparison.OrdinalIgnoreCase)))
                        {
                            return match;
                        }
                    }
                }
            }
            return null;
        }

        public Match GetMatchByTeamName(Tournament tournament, string teamName)
        {
            foreach (var round in tournament.MatchLog.MatchesToPlayByRound.Values)
            {
                foreach (var match in round)
                {
                    if ((match.TeamA != null && match.TeamA.Equals(teamName, StringComparison.OrdinalIgnoreCase)) ||
                        (match.TeamB != null && match.TeamB.Equals(teamName, StringComparison.OrdinalIgnoreCase)))
                    {
                        return match;
                    }
                }
            }
            return null;
        }

        public string GetLosingTeamName(Match match, string winningTeamName)
        {
            if (match.TeamA != null && match.TeamA.Equals(winningTeamName, StringComparison.OrdinalIgnoreCase))
            {
                return match.TeamB;
            }
            else if (match.TeamB != null && match.TeamB.Equals(winningTeamName, StringComparison.OrdinalIgnoreCase))
            {
                return match.TeamA;
            }
            return null; // No losing team found
        }

        public void BuildMatchScheduleResolver(Tournament tournament)
        {
            switch (tournament.Type)
            {
                case TournamentType.Ladder:
                    // Ladder tournaments do not have a match schedule resolver
                    break;

                case TournamentType.RoundRobin:
                    BuildRoundRobinMatchSchedule(tournament);
                    break;

                default:
                    Console.WriteLine($"Match schedule resolver not implemented for tournament type: {tournament.Type}");
                    break;
            }
        }

        public void BuildRoundRobinMatchSchedule(Tournament tournament)
        {
            const int maxRetries = 10;
            int attempt = 0;

            while (attempt < maxRetries)
            {
                attempt++;
                ClearMatchSchedule(tournament);

                var teams = tournament.Teams.Select(t => t.Name).ToList();
                bool hasBye = false;
                const string byePlaceholder = "__BYE__";
                if (teams.Count % 2 != 0)
                {
                    hasBye = true;
                    teams.Add(byePlaceholder);
                }

                int numRounds = teams.Count - 1;
                int half = teams.Count / 2;
                var rotating = new List<string>(teams); // First element fixed

                for (int round = 1; round <= numRounds; round++)
                {
                    var pairings = new List<Match>();

                    for (int i = 0; i < half; i++)
                    {
                        string a = rotating[i];
                        string b = rotating[teams.Count - 1 - i];

                        if (a == byePlaceholder && b == byePlaceholder)
                            continue;

                        bool isByeMatch = hasBye && (a == byePlaceholder || b == byePlaceholder);
                        var match = new Match(
                            a == byePlaceholder ? null : a,
                            b == byePlaceholder ? null : b)
                        {
                            IsByeMatch = isByeMatch,
                            RoundNumber = round,
                            CreatedOn = DateTime.UtcNow
                        };

                        pairings.Add(match);
                    }

                    tournament.MatchLog.MatchesToPlayByRound[round] = pairings;

                    // Rotate keeping first fixed
                    var last = rotating[^1];
                    rotating.RemoveAt(rotating.Count - 1);
                    rotating.Insert(1, last);
                }

                if (ValidateRoundRobin(tournament))
                {
                    // Valid schedule, done
                    tournament.TotalRounds = tournament.MatchLog.MatchesToPlayByRound.Count;
                    break;
                }
                else
                {
                    Console.WriteLine($"Validation failed on attempt {attempt}, retrying build...");
                }
            }

            if (attempt == maxRetries)
            {
                Console.WriteLine("Failed to build a valid round-robin schedule after max retries.");
            }
        }

        /// <summary>
        /// Validates the round robin match schedule for the given tournament.
        /// Checks for missing, duplicate, or unexpected match pairs and per-round conflicts.
        /// </summary>
        /// <param name="tournament">The tournament whose schedule to validate.</param>
        /// <returns>True if the schedule is valid; otherwise, false.</returns>
        private bool ValidateRoundRobin(Tournament tournament)
        {
            // Get distinct team names (case-insensitive)
            var teams = tournament.Teams.Select(t => t.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            // Build the set of all expected unique team pairings
            var expected = new HashSet<(string, string)>(new UnorderedPairComparer());
            for (int i = 0; i < teams.Count; i++)
                for (int j = i + 1; j < teams.Count; j++)
                    expected.Add((teams[i], teams[j]));

            // Track actual pairings, duplicates, and per-round conflicts
            var actual = new HashSet<(string, string)>(new UnorderedPairComparer());
            var duplicates = new List<(string, string)>();
            var conflicts = new List<string>();

            // Iterate through each round in the match schedule
            foreach (var kv in tournament.MatchLog.MatchesToPlayByRound.OrderBy(k => k.Key))
            {
                // Track teams seen in this round to detect conflicts
                var seenThisRound = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var match in kv.Value)
                {
                    // Skip bye matches
                    if (match.IsByeMatch)
                        continue;

                    // Check if TeamA appears more than once in this round
                    if (match.TeamA != null)
                    {
                        if (!seenThisRound.Add(match.TeamA))
                            conflicts.Add($"Round {kv.Key}: {match.TeamA} appears twice.");
                    }
                    // Check if TeamB appears more than once in this round
                    if (match.TeamB != null)
                    {
                        if (!seenThisRound.Add(match.TeamB))
                            conflicts.Add($"Round {kv.Key}: {match.TeamB} appears twice.");
                    }

                    // Add the pairing to actual set, track duplicates
                    if (match.TeamA != null && match.TeamB != null)
                    {
                        var pair = (match.TeamA, match.TeamB);
                        if (!actual.Add(pair))
                            duplicates.Add(pair);
                    }
                }
            }

            // Find missing, unexpected, and duplicate pairs
            var missing = expected.Except(actual, new UnorderedPairComparer()).ToList();
            var unexpected = actual.Except(expected, new UnorderedPairComparer()).ToList();

            // If no issues, schedule is valid
            if (!missing.Any() && !duplicates.Any() && !unexpected.Any() && !conflicts.Any())
            {
                Console.WriteLine($"{DateTime.UtcNow} - (MatchManager): Round Robin Match Schedule Build Validation was a success.");
                return true;
            }

            // Output validation issues
            Console.WriteLine("Validation issues found:");
            if (missing.Any())
                Console.WriteLine("Missing pairs: " + string.Join(", ", missing.Select(p => $"{p.Item1}-{p.Item2}")));
            if (duplicates.Any())
                Console.WriteLine("Duplicate pairs: " + string.Join(", ", duplicates.Select(p => $"{p.Item1}-{p.Item2}")));
            if (unexpected.Any())
                Console.WriteLine("Unexpected pairs: " + string.Join(", ", unexpected.Select(p => $"{p.Item1}-{p.Item2}")));
            if (conflicts.Any())
                Console.WriteLine("Per-round conflicts: " + string.Join("; ", conflicts));

            return false;
        }


        public void ClearMatchSchedule(Tournament tournament)
        {
            // Clear the match schedule for the tournament
            tournament.MatchLog.MatchesToPlayByRound.Clear();
        }

        public PostMatch CreateNewPostMatch(string winningTeamName, int winnerScore, string losingTeamName, int loserScore, DateTime originalCreationDateTime, bool wasByeMatch = false)
        {
            return new PostMatch(winningTeamName, winnerScore, losingTeamName, loserScore, originalCreationDateTime, wasByeMatch);
        }

        public void ConvertMatchToPostMatch(Tournament tournament, Match match, string winningTeamName, int winnerScore, string losingTeamName, int loserScore, bool wasByeMatch = false)
        {
            if (!tournament.MatchLog.MatchesToPlayByRound.ContainsKey(match.RoundNumber))
            {
                Console.WriteLine($"Round {match.RoundNumber} does not exist in the match schedule.");
                return;
            }
            var matchesInRound = tournament.MatchLog.MatchesToPlayByRound[match.RoundNumber];
            if (!matchesInRound.Contains(match))
            {
                Console.WriteLine("The specified match does not exist in the given round.");
                return;
            }

            // Create PostMatch
            PostMatch postMatch = CreateNewPostMatch(winningTeamName, winnerScore, losingTeamName, loserScore, match.CreatedOn, wasByeMatch);

            // Add to PostMatchesByRound
            if (!tournament.MatchLog.PostMatchesByRound.ContainsKey(match.RoundNumber))
                tournament.MatchLog.PostMatchesByRound[match.RoundNumber] = new List<PostMatch>();
            tournament.MatchLog.PostMatchesByRound[match.RoundNumber].Add(postMatch);

            // Remove from MatchesToPlayByRound
            matchesInRound.Remove(match);
            
            // If no matches left in the round or a bye match is left, remove the round entry
            if (matchesInRound.Count == 0)
            {
                tournament.MatchLog.MatchesToPlayByRound.Remove(match.RoundNumber);
                
                // If this was the last round, set IsRoundComplete to true
                tournament.IsRoundComplete = true;
                Console.WriteLine($"All matches for round {match.RoundNumber} have been completed.");
            }
        }
    }
}
