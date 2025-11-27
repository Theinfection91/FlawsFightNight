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
            foreach (var tournament in _dataManager.TournamentsDatabaseFile.NewTournaments)
            {
                if (tournament.MatchLog.ContainsMatchId(matchId))
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsPostMatchInCurrentRound(Tournament tournament, string matchId)
        {
            if (tournament.MatchLog.PostMatchesByRound.TryGetValue(tournament.CurrentRound, out var matches))
            {
                foreach (var match in matches)
                {
                    if (!string.IsNullOrEmpty(match.Id) && match.Id.Equals(matchId, StringComparison.OrdinalIgnoreCase))
                    {
                        return true; // Match found in current round
                    }
                }
            }
            return false; // Match not found in current round
        }

        public bool HasMatchBeenPlayed(Tournament tournament, string matchId)
        {
            foreach (var round in tournament.MatchLog.PostMatchesByRound.Values)
            {
                foreach (var postMatch in round)
                {
                    if (!string.IsNullOrEmpty(postMatch.Id) && postMatch.Id.Equals(matchId, StringComparison.OrdinalIgnoreCase))
                    {
                        return true; // Match has been played
                    }
                }
            }
            foreach (var postMatch in tournament.MatchLog.OpenRoundRobinPostMatches)
            {
                if (!string.IsNullOrEmpty(postMatch.Id) && postMatch.Id.Equals(matchId, StringComparison.OrdinalIgnoreCase))
                {
                    return true; // Match has been played
                }
            }
            return false; // Match not found in played matches
        }

        public bool IsGivenTeamNameInPostMatch(string teamName, PostMatch postMatch)
        {
            return (!string.IsNullOrEmpty(postMatch.Winner) && postMatch.Winner.Equals(teamName, StringComparison.OrdinalIgnoreCase)) ||
                   (!string.IsNullOrEmpty(postMatch.Loser) && postMatch.Loser.Equals(teamName, StringComparison.OrdinalIgnoreCase));
        }

        public bool IsMatchMadeForTeamResolver(Tournament tournament, string teamName)
        {
            switch (tournament.Type)
            {
                case TournamentType.Ladder:
                    return IsMatchMadeForTeamLadder(tournament, teamName);
                case TournamentType.RoundRobin:
                    switch (tournament.RoundRobinMatchType)
                    {
                        case RoundRobinMatchType.Normal:
                            return IsMatchMadeForTeamNormalRoundRobin(tournament, teamName);
                        case RoundRobinMatchType.Open:
                            // Open round robin does not have scheduled matches
                            return IsMatchMadeForTeamOpenRoundRobin(tournament, teamName);
                        default:
                            //Console.WriteLine($"Match check not implemented for round robin match type: {tournament.RoundRobinMatchType}");
                            return false;
                    }
                default:
                    return false;
            }
        }

        private bool IsMatchMadeForTeamLadder(Tournament tournament, string teamName)
        {
            foreach (var match in tournament.MatchLog.LadderMatchesToPlay)
            {
                if (match == null)
                {
                    //Console.WriteLine("Encountered null match in list, skipping.");
                    continue;
                }

                //Console.WriteLine($"Checking match: TeamA = {match.TeamA}, TeamB = {match.TeamB}");

                if (!string.IsNullOrEmpty(match.TeamA) &&
                    match.TeamA.Equals(teamName, StringComparison.OrdinalIgnoreCase))
                {
                    //Console.WriteLine($"Match found for team '{teamName}' as TeamA in round {currentRound}.");
                    return true;
                }

                if (!string.IsNullOrEmpty(match.TeamB) &&
                    match.TeamB.Equals(teamName, StringComparison.OrdinalIgnoreCase))
                {
                    //Console.WriteLine($"Match found for team '{teamName}' as TeamB in round {currentRound}.");
                    return true;
                }
            }

            //Console.WriteLine($"No match found for team '{teamName}' in round {currentRound}.");
            return false;
        }

        private bool IsMatchMadeForTeamNormalRoundRobin(Tournament tournament, string teamName)
        {
            if (tournament == null)
            {
                //Console.WriteLine("Tournament is null.");
                return false;
            }

            if (tournament.MatchLog == null)
            {
                //Console.WriteLine("MatchLog is null.");
                return false;
            }

            if (tournament.MatchLog.MatchesToPlayByRound == null)
            {
                //Console.WriteLine("MatchesToPlayByRound is null.");
                return false;
            }

            int currentRound = tournament.CurrentRound;
            //Console.WriteLine($"Checking only current round: {currentRound}");

            if (!tournament.MatchLog.MatchesToPlayByRound.TryGetValue(currentRound, out var matches))
            {
                //Console.WriteLine($"No entry for round {currentRound} in MatchesToPlayByRound.");
                return false;
            }

            if (matches == null)
            {
                //Console.WriteLine($"Match list for round {currentRound} is null.");
                return false;
            }

            foreach (var match in matches)
            {
                if (match == null)
                {
                    //Console.WriteLine("Encountered null match in list, skipping.");
                    continue;
                }

                //Console.WriteLine($"Checking match: TeamA = {match.TeamA}, TeamB = {match.TeamB}");

                if (!string.IsNullOrEmpty(match.TeamA) &&
                    match.TeamA.Equals(teamName, StringComparison.OrdinalIgnoreCase))
                {
                    //Console.WriteLine($"Match found for team '{teamName}' as TeamA in round {currentRound}.");
                    return true;
                }

                if (!string.IsNullOrEmpty(match.TeamB) &&
                    match.TeamB.Equals(teamName, StringComparison.OrdinalIgnoreCase))
                {
                    //Console.WriteLine($"Match found for team '{teamName}' as TeamB in round {currentRound}.");
                    return true;
                }
            }

            //Console.WriteLine($"No match found for team '{teamName}' in round {currentRound}.");
            return false;
        }

        private bool IsMatchMadeForTeamOpenRoundRobin(Tournament tournament, string teamName)
        {
            if (tournament == null)
            {
                //Console.WriteLine("Tournament is null.");
                return false;
            }

            if (tournament.MatchLog == null)
            {
                //Console.WriteLine("MatchLog is null.");
                return false;
            }

            if (tournament.MatchLog.OpenRoundRobinMatchesToPlay == null)
            {
                //Console.WriteLine("OpenRoundRobinMatchesToPlay is null.");
                return false;
            }

            foreach (var match in tournament.MatchLog.OpenRoundRobinMatchesToPlay)
            {
                if (match == null)
                {
                    //Console.WriteLine("Encountered null match in list, skipping.");
                    continue;
                }
                //Console.WriteLine($"Checking match: TeamA = {match.TeamA}, TeamB = {match.TeamB}");
                if (!string.IsNullOrEmpty(match.TeamA) &&
                    match.TeamA.Equals(teamName, StringComparison.OrdinalIgnoreCase))
                {
                    //Console.WriteLine($"Match found for team '{teamName}' as TeamA in open round robin.");
                    return true;
                }
                if (!string.IsNullOrEmpty(match.TeamB) &&
                    match.TeamB.Equals(teamName, StringComparison.OrdinalIgnoreCase))
                {
                    //Console.WriteLine($"Match found for team '{teamName}' as TeamB in open round robin.");
                    return true;
                }
            }
            //Console.WriteLine($"No match found for team '{teamName}' in open round robin.");
            return false;
        }

        public bool IsTieBreakerNeeded(MatchLog matchLog)
        {
            var teamWins = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            //Console.WriteLine("=== Debug: Starting Tie-Breaker Check ===");

            // Count wins for each team (single or double)
            foreach (var round in matchLog.PostMatchesByRound)
            {
                //Console.WriteLine($"Checking Round {round.Key} with {round.Value.Count} matches");

                foreach (var postMatch in round.Value)
                {
                    if (!postMatch.WasByeMatch)
                    {
                        // Treat Winner as string directly
                        string winnerKey = postMatch.Winner ?? "UNKNOWN";

                        //Console.WriteLine($"  Winner found: {winnerKey}");

                        if (!teamWins.ContainsKey(winnerKey))
                        {
                            teamWins[winnerKey] = 0;
                            //Console.WriteLine($"  -> New entry created for {winnerKey}");
                        }

                        teamWins[winnerKey]++;
                        //Console.WriteLine($"  -> {winnerKey} now has {teamWins[winnerKey]} wins");
                    }
                    else
                    {
                        //Console.WriteLine("  Skipping bye match");
                    }
                }
            }

            //Console.WriteLine("=== Debug: Final Team Wins ===");
            foreach (var kvp in teamWins)
            {
                //Console.WriteLine($"Team {kvp.Key} : {kvp.Value} wins");
            }

            // Check for ties
            var winCounts = teamWins.Values.GroupBy(w => w).ToList();
            foreach (var group in winCounts)
            {
                //Console.WriteLine($"Checking win count {group.Key} -> {group.Count()} teams");
                if (group.Count() > 1 && group.Key > 0) // Multiple teams with same non-zero wins
                {
                    //Console.WriteLine("Tie detected! Tie-breaker needed.");
                    return true;
                }
            }

            //Console.WriteLine("No ties detected. No tie-breaker needed.");
            return false;
        }

        public bool IsTieBreakerNeededForFirstPlace(MatchLogBase matchLog)
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
            foreach (var tournament in _dataManager.TournamentsDatabaseFile.Tournaments)
            {
                allMatches.AddRange(tournament.MatchLog.GetAllActiveMatches(tournament.CurrentRound));
            }
            return allMatches;
        }

        public List<PostMatch> GetAllPostMatches()
        {
            List<PostMatch> allPostMatches = new();
            foreach (var tournament in _dataManager.TournamentsDatabaseFile.Tournaments)
            {
                allPostMatches.AddRange(tournament.MatchLog.GetAllPostMatches());
            }
            return allPostMatches;
        }

        public List<PostMatch> GetAllRoundRobinPostMatches()
        {
            List<PostMatch> allPostMatches = new();
            foreach (var tournament in _dataManager.TournamentsDatabaseFile.Tournaments)
            {
                allPostMatches.AddRange(tournament.MatchLog.GetEditablePostMatches());
            }
            return allPostMatches;
        }

        public Match GetMatchFromDatabase(string matchId)
        {
            foreach (var tournament in _dataManager.TournamentsDatabaseFile.Tournaments)
            {
                var match = GetMatchByMatchIdResolver(tournament, matchId);
                if (match != null)
                {
                    return match;
                }
            }
            return null;
        }

        private Match GetMatchByIdInNormalRoundRobinTournament(Tournament tournament, string matchId)
        {
            foreach (var round in tournament.MatchLog.MatchesToPlayByRound.Values)
            {
                foreach (var match in round)
                {
                    if (!string.IsNullOrEmpty(match.Id) && match.Id.Equals(matchId, StringComparison.OrdinalIgnoreCase))
                    {
                        return match; // Match found
                    }
                }
            }
            return null; // Match ID not found
        }

        private Match GetMatchByIdInOpenRoundRobinTournament(Tournament tournament, string matchId)
        {
            foreach (var match in tournament.MatchLog.OpenRoundRobinMatchesToPlay)
            {
                if (!string.IsNullOrEmpty(match.Id) && match.Id.Equals(matchId, StringComparison.OrdinalIgnoreCase))
                {
                    return match; // Match found
                }
            }
            return null; // Match ID not found
        }

        private Match GetMatchByIdInLadderTournament(Tournament tournament, string matchId)
        {
            foreach (var match in tournament.MatchLog.LadderMatchesToPlay)
            {
                if (!string.IsNullOrEmpty(match.Id) && match.Id.Equals(matchId, StringComparison.OrdinalIgnoreCase))
                {
                    return match; // Match found
                }
            }
            return null; // Match ID not found
        }

        public Match GetMatchByMatchIdResolver(Tournament tournament, string matchId)
        {
            switch (tournament.Type)
            {
                case TournamentType.Ladder:
                    return GetMatchByIdInLadderTournament(tournament, matchId);
                case TournamentType.RoundRobin:
                    switch (tournament.RoundRobinMatchType)
                    {
                        case RoundRobinMatchType.Normal:
                            return GetMatchByIdInNormalRoundRobinTournament(tournament, matchId);
                        case RoundRobinMatchType.Open:
                            return GetMatchByIdInOpenRoundRobinTournament(tournament, matchId);
                        default:
                            //Console.WriteLine($"Match retrieval not implemented for round robin match type: {tournament.RoundRobinMatchType}");
                            return null;
                    }
                default:
                    return null;
            }
        }

        public Match GetOpenMatchByTeamNameResolver(TournamentBase tournament, string teamName)
        {
            switch (tournament.Type)
            {
                //case TournamentType.Ladder:
                //    return GetOpenMatchByTeamNameLadder(tournament, teamName);
                //case TournamentType.RoundRobin:
                //    switch (tournament.RoundRobinMatchType)
                //    {
                //        case RoundRobinMatchType.Normal:
                //            return GetOpenMatchByTeamNameNormalRoundRobin(tournament, teamName);
                //        case RoundRobinMatchType.Open:
                //            return GetOpenMatchByTeamNameOpenRoundRobin(tournament, teamName);
                //        default:
                //            //Console.WriteLine($"Match retrieval not implemented for round robin match type: {tournament.RoundRobinMatchType}");
                //            return null;
                //    }
                default:
                    return null;
            }
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
            foreach (var round in tournament.MatchLog.PostMatchesByRound.Values)
            {
                foreach (var postMatch in round)
                {
                    if (!string.IsNullOrEmpty(postMatch.Id) && postMatch.Id.Equals(matchId, StringComparison.OrdinalIgnoreCase))
                    {
                        return postMatch; // PostMatch found
                    }
                }
            }
            foreach (var postMatch in tournament.MatchLog.OpenRoundRobinPostMatches)
            {
                if (!string.IsNullOrEmpty(postMatch.Id) && postMatch.Id.Equals(matchId, StringComparison.OrdinalIgnoreCase))
                {
                    return postMatch; // PostMatch found
                }
            }
            // Ladder PostMatches
            foreach (var postMatch in tournament.MatchLog.LadderPostMatches)
            {
                if (!string.IsNullOrEmpty(postMatch.Id) && postMatch.Id.Equals(matchId, StringComparison.OrdinalIgnoreCase))
                {
                    return postMatch; // PostMatch found
                }
            }
            return null; // PostMatch ID not found
        }

        public List<string> GetTiedTeams(MatchLogBase matchLog)
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
        public bool IsChallengedTeamWithinRanks(Team challenger, Team challenged)
        {
            int rankDifference = challenger.Rank - challenged.Rank;

            // Challenger can challenge up to 2 ranks above (e.g., 6 can challenge 5 or 4, but not 3)
            if (rankDifference >= 1 && rankDifference <= 2)
            {
                return true;
            }
            return false;
        }

        public bool IsChallengePending(Tournament tournament, string challengerTeamName, string challengedTeamName)
        {
            return tournament.MatchLog.LadderMatchesToPlay.Any(m =>
                m.Challenge != null &&
                m.Challenge.Challenger.Equals(challengerTeamName, StringComparison.OrdinalIgnoreCase) &&
                m.Challenge.Challenged.Equals(challengedTeamName, StringComparison.OrdinalIgnoreCase));
        }

        public bool IsWinningTeamChallenger(Match match, Team winningTeam)
        {
            return match.Challenge != null &&
                   match.Challenge.Challenger.Equals(winningTeam.Name, StringComparison.OrdinalIgnoreCase);
        }

        public bool HasChallengeSent(TournamentBase tournament, string challengerTeamName)
        {
            return tournament.MatchLog.GetAllActiveMatches().Any(m =>
                m.Challenge != null &&
                m.Challenge.Challenger.Equals(challengerTeamName, StringComparison.OrdinalIgnoreCase));
        }

        public Match? GetChallengeMatchByChallengerName(TournamentBase tournament, string challengerTeamName)
        {
            return tournament.MatchLog.GetAllActiveMatches().FirstOrDefault(m =>
                m.Challenge != null &&
                m.Challenge.Challenger.Equals(challengerTeamName, StringComparison.OrdinalIgnoreCase));
        }

        public List<Team> GetAllChallengerTeams(List<Team> allLadderTeams)
        {
            List<Team> challengerTeams = new();
            foreach (var tournament in _dataManager.TournamentsDatabaseFile.Tournaments)
            {
                if (tournament.Type == TournamentType.Ladder)
                {
                    foreach (var match in tournament.MatchLog.LadderMatchesToPlay)
                    {
                        if (match.Challenge != null)
                        {
                            var challenger = allLadderTeams.FirstOrDefault(t => t.Name.Equals(match.Challenge.Challenger, StringComparison.OrdinalIgnoreCase));
                            if (challenger != null && !challengerTeams.Any(t => t.Name.Equals(challenger.Name, StringComparison.OrdinalIgnoreCase)))
                            {
                                challengerTeams.Add(challenger);
                            }
                        }
                    }
                }
            }
            return challengerTeams;
        }

        public List<Team> GetAllChallengeTeams(Tournament tournament)
        {
            List<Team> challengeTeams = new();
            foreach (var match in tournament.MatchLog.LadderMatchesToPlay)
            {
                if (match.Challenge != null)
                {
                    // Get challenger and challenged teams
                    var challenger = tournament.Teams.FirstOrDefault(t => t.Name.Equals(match.Challenge.Challenger, StringComparison.OrdinalIgnoreCase));
                    var challenged = tournament.Teams.FirstOrDefault(t => t.Name.Equals(match.Challenge.Challenged, StringComparison.OrdinalIgnoreCase));
                    if (challenger != null && !challengeTeams.Any(t => t.Name.Equals(challenger.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        challengeTeams.Add(challenger);
                    }
                    if (challenged != null && !challengeTeams.Any(t => t.Name.Equals(challenged.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        challengeTeams.Add(challenged);
                    }
                }
            }
            return challengeTeams;
        }

        public Match GetLadderMatchForTeam(Tournament tournament, string teamName)
        {
            foreach (var match in tournament.MatchLog.LadderMatchesToPlay)
            {
                if ((match.Challenge != null) &&
                    (match.Challenge.Challenger.Equals(teamName, StringComparison.OrdinalIgnoreCase) ||
                     match.Challenge.Challenged.Equals(teamName, StringComparison.OrdinalIgnoreCase)))
                {
                    return match;
                }
            }
            return null;
        }

        public Match CreateLadderMatchWithChallenge(Team challengerTeam, Team challengedTeam)
        {
            var match = new Match(challengerTeam.Name, challengedTeam.Name)
            {
                Id = GenerateMatchId(),
                IsByeMatch = false,
                RoundNumber = 0,
                CreatedOn = DateTime.UtcNow,
                Challenge = CreateChallenge(challengerTeam, challengedTeam)
            };
            return match;
        }

        private Challenge CreateChallenge(Team challengerTeam, Team challengedTeam)
        {
            var challenge = new Challenge(challengerTeam.Name, challengerTeam.Rank, challengedTeam.Name, challengedTeam.Rank);

            return challenge;
        }

        public void ReassignRanksInLadderTournament(Tournament tournament)
        {
            // Sort teams by their current rank
            tournament.Teams.Sort((a, b) => a.Rank.CompareTo(b.Rank));

            // Reassign ranks sequentially starting from 1
            for (int i = 0; i < tournament.Teams.Count; i++)
            {
                tournament.Teams[i].Rank = i + 1;
            }
        }

        public void ChallengeRankComparisonProcess(Tournament tournament)
        {
            var challengeTeams = GetAllChallengeTeams(tournament);
            foreach (var team in challengeTeams)
            {
                if (!IsChallengeRankCorrect(tournament, team))
                {
                    var challengeToEdit = GetLadderMatchForTeam(tournament, team.Name);
                    if (team.Name.Equals(challengeToEdit.Challenge.Challenger, StringComparison.OrdinalIgnoreCase))
                    {
                        challengeToEdit.Challenge.ChallengerRank = team.Rank;
                    }
                    else if (team.Name.Equals(challengeToEdit.Challenge.Challenged, StringComparison.OrdinalIgnoreCase))
                    {
                        challengeToEdit.Challenge.ChallengedRank = team.Rank;
                    }
                }
            }
        }

        public bool IsChallengeRankCorrect(Tournament tournament, Team team)
        {
            foreach (var match in tournament.MatchLog.LadderMatchesToPlay)
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

        private bool ValidateNormalRoundRobin(TournamentBase tournament, bool isDoubleRoundRobin)
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
        public void SendMatchSchedulesToTeamsResolver(TournamentBase tournament)
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

        private async void SendNormalRoundRobinMatchScheduleNotificationToDiscordId(ulong discordId, TournamentBase tournament)
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

        private async void SendOpenRoundRobinMatchScheduleNotificationToDiscordId(ulong discordId, TournamentBase tournament)
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


        public async void SendChallengeSuccessNotificationProcess(TournamentBase tournament, Match match, Team challengerTeam, Team challengedTeam)
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
                    var message = _embedManager.LadderSendChallengeMatchNotification(tournament, challengerTeam, challengedTeam, true);
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
                    var message = _embedManager.LadderSendChallengeMatchNotification(tournament, challengerTeam, challengedTeam, false);
                    await dmChannel.SendMessageAsync(embed: message);
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error sending ladder match challenge notifications: {ex.Message}");
            }
        }

        public async void SendChallengeCancelNotificationProcess(TournamentBase tournament, Match match, Team challengerTeam, Team challengedTeam)
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
                    var message = _embedManager.LadderCancelChallengeMatchNotification(tournament, challengerTeam, challengedTeam, true);
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
                    var message = _embedManager.LadderCancelChallengeMatchNotification(tournament, challengerTeam, challengedTeam, false);
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
            foreach (var tournament in _dataManager.TournamentsDatabaseFile.NewTournaments)
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

        public List<Match> GetMatchesForTeamNormalRoundRobin(string teamName, MatchLogBase matchLog)
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

        public List<Match> GetMatchesForTeamOpenRoundRobin(string teamName, MatchLogBase matchLog)
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

        #region Tiebreaker Methods
        public string ResolveTieBreaker(List<string> tiedTeams, MatchLog log)
        {
            //Console.WriteLine("=== Tie-Breaker Resolution Started ===");

            // Step 1: Head-to-head wins among tied teams
            var wins = tiedTeams.ToDictionary(t => t, t => 0);

            var headToHead = log.PostMatchesByRound
                .SelectMany(kvp => kvp.Value)
                .Where(pm =>
                    !pm.WasByeMatch &&
                    tiedTeams.Contains(pm.Winner) &&
                    tiedTeams.Contains(pm.Loser))
                .ToList();

            //Console.Write.WriteLine($"Step 1: Head-to-head matches found: {headToHead.Count}");
            foreach (var pm in headToHead)
            {
                wins[pm.Winner]++;
                //Console.Write.WriteLine($"  {pm.Winner} beat {pm.Loser} ({pm.WinnerScore}-{pm.LoserScore})");
            }

            int maxWins = wins.Values.Max();
            var leaders = wins.Where(w => w.Value == maxWins).Select(w => w.Key).ToList();
            //Console.Write.WriteLine($"  Leaders after Step 1 (max wins = {maxWins}): {string.Join(", ", leaders)}");
            if (leaders.Count == 1)
            {
                //Console.Write.WriteLine($"Tie-breaker resolved by head-to-head wins → Winner: {leaders.First()}");
                return leaders.First();
            }

            // Step 2: Point differential among tied teams
            var pointDiff = leaders.ToDictionary(t => t, t => 0);
            foreach (var pm in headToHead)
            {
                if (leaders.Contains(pm.Winner))
                {
                    int diffWinner = pm.WinnerScore - pm.LoserScore;
                    pointDiff[pm.Winner] += diffWinner;
                    //Console.Write.WriteLine($"  Point diff for {pm.Winner}: {diffWinner:+#;-#;0}");
                }

                if (leaders.Contains(pm.Loser))
                {
                    int diffLoser = pm.LoserScore - pm.WinnerScore;
                    pointDiff[pm.Loser] += diffLoser;
                    //Console.Write.WriteLine($"  Point diff for {pm.Loser}: {diffLoser:+#;-#;0}");
                }
            }

            int maxDiff = pointDiff.Values.Max();
            var leadersByDiff = pointDiff.Where(p => p.Value == maxDiff).Select(p => p.Key).ToList();
            //Console.Write.WriteLine($"  Leaders after Step 2 (max point diff = {maxDiff}): {string.Join(", ", leadersByDiff)}");
            if (leadersByDiff.Count == 1)
            {
                //Console.Write.WriteLine($"Tie-breaker resolved by point differential → Winner: {leadersByDiff.First()}");
                return leadersByDiff.First();
            }

            // Step 3: Total points scored vs other tied teams
            var totalPointsVsTied = leadersByDiff.ToDictionary(t => t, t => 0);
            foreach (var pm in headToHead)
            {
                if (totalPointsVsTied.ContainsKey(pm.Winner))
                {
                    totalPointsVsTied[pm.Winner] += pm.WinnerScore;
                    //Console.Write.WriteLine($"  Total points vs tied teams for {pm.Winner}: +{pm.WinnerScore}");
                }

                if (totalPointsVsTied.ContainsKey(pm.Loser))
                {
                    totalPointsVsTied[pm.Loser] += pm.LoserScore;
                    //Console.Write.WriteLine($"  Total points vs tied teams for {pm.Loser}: +{pm.LoserScore}");
                }
            }

            int maxPointsVsTied = totalPointsVsTied.Values.Max();
            var leadersByPointsVsTied = totalPointsVsTied
                .Where(p => p.Value == maxPointsVsTied)
                .Select(p => p.Key)
                .ToList();
            //Console.Write.WriteLine($"  Leaders after Step 3 (max points vs tied = {maxPointsVsTied}): {string.Join(", ", leadersByPointsVsTied)}");
            if (leadersByPointsVsTied.Count == 1)
            {
                //Console.Write.WriteLine($"Tie-breaker resolved by total points vs tied teams → Winner: {leadersByPointsVsTied.First()}");
                return leadersByPointsVsTied.First();
            }

            // Step 4: Total points scored against all teams
            var allMatches = log.PostMatchesByRound
                .SelectMany(kvp => kvp.Value)
                .Where(pm => !pm.WasByeMatch);

            var totalPointsOverall = leadersByPointsVsTied.ToDictionary(t => t, t => 0);
            foreach (var pm in allMatches)
            {
                if (totalPointsOverall.ContainsKey(pm.Winner))
                {
                    totalPointsOverall[pm.Winner] += pm.WinnerScore;
                    //Console.Write.WriteLine($"  Total points overall for {pm.Winner}: +{pm.WinnerScore}");
                }

                if (totalPointsOverall.ContainsKey(pm.Loser))
                {
                    totalPointsOverall[pm.Loser] += pm.LoserScore;
                    //Console.Write.WriteLine($"  Total points overall for {pm.Loser}: +{pm.LoserScore}");
                }
            }

            int maxPointsOverall = totalPointsOverall.Values.Max();
            var leadersByPointsOverall = totalPointsOverall
                .Where(p => p.Value == maxPointsOverall)
                .Select(p => p.Key)
                .ToList();
            //Console.Write.WriteLine($"  Leaders after Step 4 (max points overall = {maxPointsOverall}): {string.Join(", ", leadersByPointsOverall)}");
            if (leadersByPointsOverall.Count == 1)
            {
                //Console.Write.WriteLine($"Tie-breaker resolved by total points overall → Winner: {leadersByPointsOverall.First()}");
                return leadersByPointsOverall.First();
            }

            // Step 5: Least amount of points against
            var pointsAgainst = leadersByPointsOverall.ToDictionary(t => t, t => 0);
            foreach (var p in pointsAgainst)
            {
                var (forPoints, againstPoints) = log.GetPointsForAndAgainst(p.Key);
                pointsAgainst[p.Key] = againstPoints;
                //Console.Write.WriteLine($"  Points against for {p.Key}: {againstPoints}");
            }
            int minPointsAgainst = pointsAgainst.Values.Min();
            var leadersByLeastPointsAgainst = pointsAgainst
                .Where(p => p.Value == minPointsAgainst)
                .Select(p => p.Key)
                .ToList();
            //Console.Write.WriteLine($"  Leaders after Step 5 (min points against = {minPointsAgainst}): {string.Join(", ", leadersByLeastPointsAgainst)}");
            if (leadersByLeastPointsAgainst.Count == 1)
            {
                //Console.Write.WriteLine($"Tie-breaker resolved by least points against → Winner: {leadersByLeastPointsAgainst.First()}");
                return leadersByLeastPointsAgainst.First();
            }

            // Step 6: Still tied — fallback random selection
            var chosen = leadersByPointsOverall.OrderBy(_ => Guid.NewGuid()).First();
            //Console.Write.WriteLine($"Tie-breaker unresolved by all criteria → Randomly selected: {chosen}");
            return chosen;
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
            var allMatches = tournament.MatchLog.PostMatchesByRound.Values.SelectMany(v => v)
                .Concat(tournament.MatchLog.OpenRoundRobinPostMatches)
                .OrderByDescending(pm => pm.CreatedOn)
                .ToList();

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
