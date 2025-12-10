using Discord.WebSocket;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Core.Models.MatchLogs;
using FlawsFightNight.Core.Models.Tournaments;
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

        private readonly DiscordSocketClient _client;
        private EmbedManager _embedManager;

        public MatchManager(DataManager dataManager, DiscordSocketClient client, EmbedManager embedManager) : base("MatchManager", dataManager)
        {
            _client = client;
            _embedManager = embedManager;
        }

        public string? GenerateMatchId()
        {
            bool isUnique = false;
            string uniqueId;

            while (!isUnique)
            {
                Random random = new();
                int randomInt = random.Next(100, 1000);
                uniqueId = $"M{randomInt}";

                // Check if the generated ID is unique
                if (!IsMatchIdInDatabase(uniqueId))
                {
                    isUnique = true;
                    return uniqueId;
                }
            }
            return null;
        }

        #region Bools
        public bool IsMatchIdInDatabase(string matchId)
        {
            foreach (var tournament in _dataManager.TournamentsDatabaseFile.Tournaments)
            {
                if (tournament.MatchLog.ContainsMatchId(matchId))
                {
                    return true;
                }
            }
            return false;
        }

        public bool HasMatchBeenPlayed(Tournament tournament, string matchId)
        {
            foreach (var match in tournament.MatchLog.GetAllPostMatches())
            {
                if (!string.IsNullOrEmpty(match.Id) && match.Id.Equals(matchId, StringComparison.OrdinalIgnoreCase))
                {
                    return true; // Match found in played matches
                }
            }
            return false; // Match not found in played matches
        }

        public bool IsGivenTeamNameInPostMatch(string teamName, PostMatch postMatch)
        {
            return (!string.IsNullOrEmpty(postMatch.Winner) && postMatch.Winner.Equals(teamName, StringComparison.OrdinalIgnoreCase)) ||
                   (!string.IsNullOrEmpty(postMatch.Loser) && postMatch.Loser.Equals(teamName, StringComparison.OrdinalIgnoreCase));
        }

        public bool IsTieBreakerNeededForFirstPlace(MatchLog matchLog)
        {
            var teamWins = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            // Count wins
            foreach (var match in matchLog.GetAllPostMatches())
            {
                if (!match.WasByeMatch)
                {
                    string winner = match.Winner;
                    if (!teamWins.ContainsKey(winner))
                    {
                        teamWins[winner] = 0;
                    }
                    teamWins[winner]++;
                }
            }

            if (teamWins.Count == 0)
                return false; // no matches played

            // Find the highest win count
            int maxWins = teamWins.Values.Max();

            // Count how many teams have that max
            int teamsWithMax = teamWins.Count(kvp => kvp.Value == maxWins);

            // A tiebreaker is only needed if more than 1 team has the top score
            return teamsWithMax > 1;
        }

        public bool IsTeamInMatch(Match match, string teamName)
        {
            return (!string.IsNullOrEmpty(match.TeamA) && match.TeamA.Equals(teamName, StringComparison.OrdinalIgnoreCase)) ||
                   (!string.IsNullOrEmpty(match.TeamB) && match.TeamB.Equals(teamName, StringComparison.OrdinalIgnoreCase));
        }
        #endregion

        #region Gets
        public List<Match> GetAllActiveMatches()
        {
            List<Match> allMatches = new();
            foreach (var tournament in _dataManager.TournamentDataFiles.Select(df => df.Tournament))
            {
                allMatches.AddRange(tournament.MatchLog.GetAllActiveMatches());
            }
            return allMatches;
        }

        public List<PostMatch> GetAllPostMatches()
        {
            List<PostMatch> allPostMatches = new();
            foreach (var tournament in _dataManager.TournamentDataFiles.Select(df => df.Tournament))
            {
                allPostMatches.AddRange(tournament.MatchLog.GetAllPostMatches());
            }
            return allPostMatches;
        }

        public List<PostMatch> GetAllRoundRobinPostMatches()
        {
            List<PostMatch> allPostMatches = new();
            foreach (var tournament in _dataManager.TournamentDataFiles.Select(df => df.Tournament))
            {
                if (tournament is NormalRoundRobinTournament || tournament is OpenRoundRobinTournament)
                {
                    allPostMatches.AddRange(tournament.MatchLog.GetAllPostMatches());
                }
            }
            return allPostMatches;
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

        public PostMatch GetPostMatchByIdInTournament(Tournament tournament, string matchId)
        {
            foreach (var match in tournament.MatchLog.GetAllPostMatches())
            {
                if (!string.IsNullOrEmpty(match.Id) && match.Id.Equals(matchId, StringComparison.OrdinalIgnoreCase))
                {
                    return match; // Match found
                }
            }
            return null; // Match ID not found
        }

        public List<string> GetTiedTeams(MatchLog matchLog)
        {
            var teamWins = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            // Count wins
            foreach (var match in matchLog.GetAllPostMatches())
            {
                if (!match.WasByeMatch)
                {
                    string winner = match.Winner;
                    if (!teamWins.ContainsKey(winner))
                    {
                        teamWins[winner] = 0;
                    }
                    teamWins[winner]++;
                }
            }

            if (teamWins.Count == 0)
                return new List<string>();

            // Find the max win count
            int maxWins = teamWins.Values.Max();

            // Collect only teams tied at that top win count
            var topTiedTeams = teamWins
                .Where(kvp => kvp.Value == maxWins)
                .Select(kvp => kvp.Key)
                .ToList();

            // Only return if there's an actual tie (2 or more teams at max)
            return topTiedTeams.Count > 1 ? topTiedTeams : new List<string>();
        }
        #endregion

        #region Ladder Challenge Methods
        public bool IsWinningTeamChallenger(Match match, Team winningTeam)
        {
            return match.Challenge != null &&
                   match.Challenge.Challenger.Equals(winningTeam.Name, StringComparison.OrdinalIgnoreCase);
        }

        public bool HasChallengeSent(Tournament tournament, string challengerTeamName)
        {
            return tournament.MatchLog.GetAllActiveMatches().Any(m =>
                m.Challenge != null &&
                m.Challenge.Challenger.Equals(challengerTeamName, StringComparison.OrdinalIgnoreCase));
        }

        public Match? GetChallengeMatchByChallengerName(Tournament tournament, string challengerTeamName)
        {
            return tournament.MatchLog.GetAllActiveMatches().FirstOrDefault(m =>
                m.Challenge != null &&
                m.Challenge.Challenger.Equals(challengerTeamName, StringComparison.OrdinalIgnoreCase));
        }

        public Match CreateChallengeMatch(Team challengerTeam, Team challengedTeam, int challengerRating = 0, int challengedRating = 0)
        {
            var match = new Match(challengerTeam.Name, challengedTeam.Name)
            {
                Id = GenerateMatchId(),
                IsByeMatch = false,
                RoundNumber = 0,
                CreatedOn = DateTime.UtcNow,
                Challenge = CreateChallenge(challengerTeam, challengedTeam, challengerRating, challengedRating)
            };
            return match;
        }

        private Challenge CreateChallenge(Team challengerTeam, Team challengedTeam, int challengerRating, int challengedRating)
        {
            var challenge = new Challenge(challengerTeam.Name, challengerTeam.Rank, challengedTeam.Name, challengedTeam.Rank)
            {
                ChallengerRating = challengerRating,
                ChallengedRating = challengedRating
            };

            return challenge;
        }
        #endregion

        #region Match/PostMatch Schedule Build/Validate/Clear
        public void BuildRoundRobinMatchSchedule(NormalRoundRobinTournament tournament)
        {
            const int maxRetries = 10;
            int attempt = 0;

            while (attempt < maxRetries)
            {
                attempt++;
                tournament.MatchLog.ClearLog();

                var teams = tournament.Teams.Select(t => t.Name).ToList();
                bool hasBye = false;
                const string byePlaceholder = "BYE";
                if (teams.Count % 2 != 0)
                {
                    hasBye = true;
                    teams.Add(byePlaceholder);
                }

                int numRounds = teams.Count - 1;
                int half = teams.Count / 2;
                var rotating = new List<string>(teams); // first element fixed

                // Single Round Robin Logic
                for (int round = 1; round <= numRounds; round++)
                {
                    var pairings = new List<Match>();
                    for (int i = 0; i < half; i++)
                    {
                        string a = rotating[i];
                        string b = rotating[teams.Count - 1 - i];
                        if (a == byePlaceholder && b == byePlaceholder) continue;

                        bool isByeMatch = hasBye && (a == byePlaceholder || b == byePlaceholder);
                        var match = new Match(
                            a == byePlaceholder ? "BYE" : a,
                            b == byePlaceholder ? "BYE" : b)
                        {
                            Id = GenerateMatchId(),
                            IsByeMatch = isByeMatch,
                            RoundNumber = round,
                            CreatedOn = DateTime.UtcNow
                        };
                        pairings.Add(match);
                    }

                    if (tournament.MatchLog is NormalRoundRobinMatchLog normalLog)
                    {
                        normalLog.MatchesToPlayByRound[round] = pairings;
                    }

                    // rotate teams, first element fixed
                    var last = rotating[^1];
                    rotating.RemoveAt(rotating.Count - 1);
                    rotating.Insert(1, last);
                }

                // Double Round Robin Logic
                if (tournament.IsDoubleRoundRobin)
                {
                    if (tournament.MatchLog is NormalRoundRobinMatchLog normalLog)
                    {
                        int currentMaxRound = normalLog.MatchesToPlayByRound.Count;
                        for (int round = 1; round <= currentMaxRound; round++)
                        {
                            var original = normalLog.MatchesToPlayByRound[round];
                            var reversed = original.Select(m => new Match(m.TeamB, m.TeamA)
                            {
                                Id = GenerateMatchId(),
                                IsByeMatch = m.IsByeMatch,
                                RoundNumber = round + currentMaxRound,
                                CreatedOn = DateTime.UtcNow
                            }).ToList();

                            normalLog.MatchesToPlayByRound[round + currentMaxRound] = reversed;
                        }
                    }
                }

                if (ValidateNormalRoundRobin(tournament, tournament.IsDoubleRoundRobin))
                {
                    if (tournament.MatchLog is NormalRoundRobinMatchLog normalLog)
                        tournament.TotalRounds = normalLog.MatchesToPlayByRound.Count;
                    break;
                }
                else
                {
                    //Console.WriteLine($"Validation failed on attempt {attempt}, retrying build...");
                }
            }
            if (attempt == maxRetries)
            {
                //Console.WriteLine("Failed to build a valid round-robin schedule after max retries.");
            }
        }

        public void BuildRoundRobinMatchSchedule(OpenRoundRobinTournament tournament)
        {
            if (tournament.MatchLog is OpenRoundRobinMatchLog openLog)
            {
                var teams = tournament.Teams.Select(t => t.Name).ToList();

                // generate all unique pairings
                var matches = new List<Match>();
                for (int i = 0; i < teams.Count; i++)
                {
                    for (int j = i + 1; j < teams.Count; j++)
                    {
                        var match = new Match(teams[i], teams[j])
                        {
                            Id = GenerateMatchId(),
                            IsByeMatch = false,
                            RoundNumber = 0,
                            CreatedOn = DateTime.UtcNow
                        };
                        matches.Add(match);
                    }
                }

                // Add matches to the open list
                openLog.MatchesToPlay.AddRange(matches);

                // If double round robin, add reversed pairings
                if (tournament.IsDoubleRoundRobin)
                {
                    var reversed = matches.Select(m => new Match(m.TeamB, m.TeamA)
                    {
                        Id = GenerateMatchId(),
                        IsByeMatch = false,
                        RoundNumber = 0,
                        CreatedOn = DateTime.UtcNow
                    }).ToList();

                    openLog.MatchesToPlay.AddRange(reversed);
                }
            }
        }

        private bool ValidateNormalRoundRobin(Tournament tournament, bool isDoubleRoundRobin)
        {
            var teams = tournament.Teams.Select(t => t.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            // Build the set of all expected unique team pairings
            var expected = new HashSet<(string, string)>(new UnorderedPairComparer());
            for (int i = 0; i < teams.Count; i++)
                for (int j = i + 1; j < teams.Count; j++)
                    expected.Add((teams[i], teams[j]));

            var actual = new HashSet<(string, string)>(new UnorderedPairComparer());
            var conflicts = new List<string>();

            if (tournament.MatchLog is NormalRoundRobinMatchLog normalLog)
            {
                foreach (var kv in normalLog.MatchesToPlayByRound.OrderBy(k => k.Key))
                {
                    var seenThisRound = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var match in kv.Value)
                    {
                        if (match.IsByeMatch) continue;

                        if (match.TeamA != null && !seenThisRound.Add(match.TeamA))
                            conflicts.Add($"Round {kv.Key}: {match.TeamA} appears twice.");
                        if (match.TeamB != null && !seenThisRound.Add(match.TeamB))
                            conflicts.Add($"Round {kv.Key}: {match.TeamB} appears twice.");

                        if (match.TeamA != null && match.TeamB != null)
                            actual.Add((match.TeamA, match.TeamB));
                    }
                }

                // For double round robin, allow each pair to appear twice
                var pairCounts = new Dictionary<(string, string), int>(new UnorderedPairComparer());
                foreach (var pair in actual)
                {
                    if (!pairCounts.ContainsKey(pair)) pairCounts[pair] = 0;
                    pairCounts[pair]++;
                }

                var duplicates = new List<(string, string)>();
                foreach (var kvp in pairCounts)
                {
                    int allowed = isDoubleRoundRobin ? 2 : 1;
                    if (kvp.Value > allowed)
                        duplicates.Add(kvp.Key);
                }

                var missing = expected.Except(actual, new UnorderedPairComparer()).ToList();
                var unexpected = actual.Except(expected, new UnorderedPairComparer()).ToList();

                if (!missing.Any() && !duplicates.Any() && !unexpected.Any() && !conflicts.Any())
                    return true; // No issues, silent success

                // Only print actual errors
                //if (missing.Any()) Console.WriteLine("Missing pairs: " + string.Join(", ", missing.Select(p => $"{p.Item1}-{p.Item2}")));
                //if (duplicates.Any()) Console.WriteLine("Duplicate pairs: " + string.Join(", ", duplicates.Select(p => $"{p.Item1}-{p.Item2}")));
                //if (unexpected.Any()) Console.WriteLine("Unexpected pairs: " + string.Join(", ", unexpected.Select(p => $"{p.Item1}-{p.Item2}")));
                //if (conflicts.Any()) Console.WriteLine("Per-round conflicts: " + string.Join("; ", conflicts));

            }
            return false;
        }
        #endregion

        #region Match Message System
        public void SendMatchSchedulesToTeamsResolver(Tournament tournament)
        {
            if (tournament is NormalRoundRobinTournament)
            {
                foreach (var team in tournament.Teams)
                {
                    foreach (var user in team.Members)
                    {
                        SendNormalRoundRobinMatchScheduleNotificationToDiscordId(user.DiscordId, tournament);
                    }
                }
            }
            else if (tournament is OpenRoundRobinTournament)
            {
                foreach (var team in tournament.Teams)
                {
                    foreach (var user in team.Members)
                    {
                        SendOpenRoundRobinMatchScheduleNotificationToDiscordId(user.DiscordId, tournament);
                    }
                }
            }
        }

        private async void SendNormalRoundRobinMatchScheduleNotificationToDiscordId(ulong discordId, Tournament tournament)
        {
            // This is a fire-and-forget method. Errors are logged but not thrown.
            try
            {
                var user = await _client.GetUserAsync(discordId);
                if (user == null)
                {
                    //Console.WriteLine($"User with Discord ID {discordId} not found.");
                    return;
                }

                // Check if the user is a bot
                if (user.IsBot)
                {
                    return;
                }

                // Grab matches
                var teamName = GetTeamNameFromDiscordId(user.Id, tournament.Id);
                var matches = GetMatchesForTeamNormalRoundRobin(teamName, tournament.MatchLog);

                var dmChannel = await user.CreateDMChannelAsync();
                if (dmChannel == null)
                {
                    //Console.WriteLine($"Failed to create DM channel with user {user.Username}.");
                    return;
                }
                var message = _embedManager.NormalRoundRobinMatchScheduleNotification(tournament as NormalRoundRobinTournament, matches, user.Username, discordId, teamName);
                await dmChannel.SendMessageAsync(embed: message);
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error sending DM to user with Discord ID {discordId}: {ex.Message}");
            }
        }

        private async void SendOpenRoundRobinMatchScheduleNotificationToDiscordId(ulong discordId, Tournament tournament)
        {
            // This is a fire-and-forget method. Errors are logged but not thrown.
            try
            {
                var user = await _client.GetUserAsync(discordId);
                if (user == null)
                {
                    //Console.WriteLine($"User with Discord ID {discordId} not found.");
                    return;
                }

                // Check if the user is a bot
                if (user.IsBot)
                {
                    return;
                }

                // Grab matches
                var teamName = GetTeamNameFromDiscordId(user.Id, tournament.Id);
                var matches = GetMatchesForTeamOpenRoundRobin(teamName, tournament.MatchLog);

                var dmChannel = await user.CreateDMChannelAsync();
                if (dmChannel == null)
                {
                    //Console.WriteLine($"Failed to create DM channel with user {user.Username}.");
                    return;
                }
                var message = _embedManager.OpenRoundRobinMatchScheduleNotification(tournament, matches, user.Username, discordId, teamName);
                await dmChannel.SendMessageAsync(embed: message);
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error sending DM to user with Discord ID {discordId}: {ex.Message}");
            }
        }


        public async void SendChallengeSuccessNotificationProcess(Tournament tournament, Match match, Team challengerTeam, Team challengedTeam)
        {
            try
            {
                foreach (var member in challengerTeam.Members)
                {
                    var user = await _client.GetUserAsync(member.DiscordId);
                    if (user == null || user.IsBot)
                    {
                        continue;
                    }
                    var dmChannel = await user.CreateDMChannelAsync();
                    if (dmChannel == null)
                    {
                        continue;
                    }
                    var message = _embedManager.SendLadderChallengeMatchNotificationResolver(tournament, challengerTeam, challengedTeam, true);
                    await dmChannel.SendMessageAsync(embed: message);
                }
                foreach (var member in challengedTeam.Members)
                {
                    var user = await _client.GetUserAsync(member.DiscordId);
                    if (user == null || user.IsBot)
                    {
                        continue;
                    }
                    var dmChannel = await user.CreateDMChannelAsync();
                    if (dmChannel == null)
                    {
                        continue;
                    }
                    var message = _embedManager.SendLadderChallengeMatchNotificationResolver(tournament, challengerTeam, challengedTeam, false);
                    await dmChannel.SendMessageAsync(embed: message);
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error sending ladder match challenge notifications: {ex.Message}");
            }
        }

        public async void SendChallengeCancelNotificationProcess(Tournament tournament, Match match, Team challengerTeam, Team challengedTeam)
        {
            try
            {
                foreach (var member in challengerTeam.Members)
                {
                    var user = await _client.GetUserAsync(member.DiscordId);
                    if (user == null || user.IsBot)
                    {
                        continue;
                    }
                    var dmChannel = await user.CreateDMChannelAsync();
                    if (dmChannel == null)
                    {
                        continue;
                    }
                    var message = _embedManager.CancelLadderChallengeMatchNotificationResolver(tournament, challengerTeam, challengedTeam, true);
                    await dmChannel.SendMessageAsync(embed: message);
                }
                foreach (var member in challengedTeam.Members)
                {
                    var user = await _client.GetUserAsync(member.DiscordId);
                    if (user == null || user.IsBot)
                    {
                        continue;
                    }
                    var dmChannel = await user.CreateDMChannelAsync();
                    if (dmChannel == null)
                    {
                        continue;
                    }
                    var message = _embedManager.CancelLadderChallengeMatchNotificationResolver(tournament, challengerTeam, challengedTeam, false);
                    await dmChannel.SendMessageAsync(embed: message);
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error sending ladder match challenge cancellation notifications: {ex.Message}");
            }
        }

        public string GetTeamNameFromDiscordId(ulong discordId, string tournamentId)
        {
            foreach (var tournament in _dataManager.TournamentDataFiles.Select(df => df.Tournament))
            {
                foreach (var team in tournament.Teams)
                {
                    foreach (var member in team.Members)
                    {
                        if (member.DiscordId.Equals(discordId) && tournament.Id.Equals(tournamentId, StringComparison.OrdinalIgnoreCase))
                        {
                            return team.Name;
                        }
                    }
                }
            }
            return "null";
        }

        public List<Match> GetMatchesForTeamNormalRoundRobin(string teamName, MatchLog matchLog)
        {
            var matches = new List<Match>();
            foreach (var match in matchLog.GetAllActiveMatches())
            {
                if ((match.TeamA != null && match.TeamA.Equals(teamName, StringComparison.OrdinalIgnoreCase)) ||
                    (match.TeamB != null && match.TeamB.Equals(teamName, StringComparison.OrdinalIgnoreCase)))
                {
                    matches.Add(match);
                    matches = matches.OrderBy(m => m.RoundNumber).ToList();
                }
            }
            return matches;
        }

        public List<Match> GetMatchesForTeamOpenRoundRobin(string teamName, MatchLog matchLog)
        {
            var matches = new List<Match>();
            foreach (var match in matchLog.GetAllActiveMatches())
            {
                if ((match.TeamA != null && match.TeamA.Equals(teamName, StringComparison.OrdinalIgnoreCase)) ||
                    (match.TeamB != null && match.TeamB.Equals(teamName, StringComparison.OrdinalIgnoreCase)))
                {
                    matches.Add(match);
                }
            }
            return matches;
        }
        #endregion

        #region Edit Match Helpers
        public void RecalculateAllWinLossStreaks(Tournament tournament)
        {
            // Reset all team streaks
            foreach (var team in tournament.Teams)
            {
                team.WinStreak = 0;
                team.LoseStreak = 0;
            }

            // Flatten all matches, order most recent first
            //var allMatches = tournament.MatchLog.PostMatchesByRound.Values.SelectMany(v => v)
            //    .Concat(tournament.MatchLog.OpenRoundRobinPostMatches)
            //    .OrderByDescending(pm => pm.CreatedOn)
            //    .ToList();

            var allMatches = tournament.MatchLog.GetAllPostMatches().OrderByDescending(pm => pm.CreatedOn);

            // For each team, calculate streak starting from the most recent match
            foreach (var team in tournament.Teams)
            {
                foreach (var match in allMatches.Where(m =>
                    m.Winner.Equals(team.Name, StringComparison.OrdinalIgnoreCase) ||
                    m.Loser.Equals(team.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    if (match.Winner.Equals(team.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        team.WinStreak++;
                        // streak continues only if next is also a win
                    }
                    else if (match.Loser.Equals(team.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        team.LoseStreak++;
                        // streak continues only if next is also a loss
                    }
                    else
                    {
                        break; // shouldn't happen, but safe guard
                    }

                    // break out if streak was broken
                    var lastWasWin = match.Winner.Equals(team.Name, StringComparison.OrdinalIgnoreCase);
                    var nextMatch = allMatches.FirstOrDefault(m =>
                        (m.Winner.Equals(team.Name, StringComparison.OrdinalIgnoreCase) ||
                         m.Loser.Equals(team.Name, StringComparison.OrdinalIgnoreCase)) &&
                        m.CreatedOn < match.CreatedOn);

                    if (nextMatch != null &&
                        ((lastWasWin && nextMatch.Loser.Equals(team.Name, StringComparison.OrdinalIgnoreCase)) ||
                         (!lastWasWin && nextMatch.Winner.Equals(team.Name, StringComparison.OrdinalIgnoreCase))))
                    {
                        break; // streak broken
                    }
                }
            }
        }
        #endregion
    }
}
