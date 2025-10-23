using Discord;
using Discord.WebSocket;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Managers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Bot.Autocomplete
{
    public class AutocompleteCache
    {
        // Add Managers as needed here
        private MatchManager _matchManager;
        private TeamManager _teamManager;
        private TournamentManager _tournamentManager;

        // Autocomplete Data
        private List<Match> _allMatches = new();
        private List<PostMatch> _allPostMatches = new();
        private List<Tournament> _allTournaments = new();
        private List<Tournament> _ladderTournaments = new();
        private List<Tournament> _roundRobinTournaments = new();
        private List<Tournament> _eliminationTournaments = new();
        private List<Team> _ladderTeams = new();
        private List<Team> _roundRobinTeams = new();


        public AutocompleteCache(MatchManager matchManager, TeamManager teamManager, TournamentManager tournamentManager)
        {
            // Initialize Managers here
            _matchManager = matchManager;
            _teamManager = teamManager;
            _tournamentManager = tournamentManager;

            // Initialize Autocomplete Data
            UpdateCache();
        }

        //public Task InitializeAsync()
        //{
        //    var client = _services.GetRequiredService<DiscordSocketClient>();
        //    //client.AutocompleteExecuted += HandleAutoCompleteAsync;
        //    return Task.CompletedTask;
        //}

        public void UpdateCache()
        {
            Console.WriteLine("[AutocompleteCache] Updating autocomplete data...");
            // Refresh autocomplete data from managers
            _allMatches = _matchManager.GetAllActiveMatches();
            _allPostMatches = _matchManager.GetAllPostMatches();
            _allTournaments = _tournamentManager.GetAllTournaments();
            _ladderTournaments = _tournamentManager.GetAllLadderTournaments();
            _roundRobinTournaments = _tournamentManager.GetAllRoundRobinTournaments();
            //_eliminationTournaments = _tournamentManager.GetAllEliminationTournaments();
            _ladderTeams = _teamManager.GetAllLadderTeams();
            _roundRobinTeams = _teamManager.GetAllRoundBasedTeams();
        }

        //private async Task HandleAutoCompleteAsync(SocketAutocompleteInteraction interaction)
        //{
        //    try
        //    {
        //        // Grab focused option and its value, set to lowercase for comparison
        //        var focusedOption = interaction.Data.Current;
        //        var value = (focusedOption.Value as string ?? string.Empty).ToLower();

        //        // Create IEnumerable to hold suggestion results
        //        IEnumerable<AutocompleteResult> results = Enumerable.Empty<AutocompleteResult>();

        //        // Limit to maximum of 25 results
        //        results = results.Take(25);

        //        // Respond with suggestions
        //        await interaction.RespondAsync(results);
                //        switch (focusedOption.Name)
                //        {
                //            case "challenger_team":
                //                return string.IsNullOrWhiteSpace(input)
                //                    ? GetTeamsForCancelChallenge("")
                //                    : GetTeamsForCancelChallenge(input);

                //            case "challenger_team_name":
                //                return string.IsNullOrWhiteSpace(input)
                //                    ? GetTeamsForSendChallenge("")
                //                    : GetTeamsForSendChallenge(input);

                //            case "challenged_team":
                //                var challengerTeamName =
                //                    interaction.Data.Options.FirstOrDefault(o => o.Name == "challenger_team_name")?.Value as string;
                //                if (string.IsNullOrWhiteSpace(challengerTeamName))
                //                    return new List<AutocompleteResult>();

                //                return string.IsNullOrWhiteSpace(input)
                //                    ? GetTeamsForSendChallenge("").Where(t => !Equals(t.Value, challengerTeamName)).ToList()
                //                    : GetTeamsForSendChallenge(input).Where(t => !Equals(t.Value, challengerTeamName)).ToList();

                //            case "ladder_team_name":
                //                return string.IsNullOrWhiteSpace(input)
                //                    ? GetLadderTeamsFromName("")
                //                    : GetLadderTeamsFromName(input);

                //            case "match_id":
                //                return string.IsNullOrWhiteSpace(input)
                //                    ? GetMatchIdsMatchingInput("")
                //                    : GetMatchIdsMatchingInput(input);

                //            case "tournament_id":
                //                return string.IsNullOrWhiteSpace(input)
                //                    ? GetTournamentIdsMatchingInput("")
                //                    : GetTournamentIdsMatchingInput(input);

                //            case "post_match_id":
                //                return string.IsNullOrWhiteSpace(input)
                //                    ? GetPostMatchIdsMatchingInput("")
                //                    : GetPostMatchIdsMatchingInput(input);

                //            case "r_tournament_id":
                //                return string.IsNullOrWhiteSpace(input)
                //                    ? GetRoundBasedTournamentIdsMatchingInput("")
                //                    : GetRoundBasedTournamentIdsMatchingInput(input);

                //            default:
                //                return new List<AutocompleteResult>();
                //        }

                //    // timeout guard — only wait up to 2.5 seconds
                //    if (await Task.WhenAny(task, Task.Delay(2500)) == task)
                //    {
                //        suggestions = task.Result ?? new();
                //    }
                //    else
                //    {
                //        Console.WriteLine("[Autocomplete] Timeout - defaulting to empty suggestions");
                //        suggestions = new();
                //    }

                //    // respond safely — only once
                //    await interaction.RespondAsync(suggestions);
                //catch (Exception ex)
                //    {
                //        Console.WriteLine($"[Autocomplete] Exception: {ex}");
                //        try
                //        {
                //            // ensure Discord receives *something* so it doesn’t hang
                //            await interaction.RespondAsync(new List<AutocompleteResult>());
                //        }
                //        catch { /* ignore double response */ }
                //    }
                //}
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //    }
        //}

        public List<AutocompleteResult> GetMatchIdsMatchingInput(string input)
        {
            // If the input is empty or only whitespace, return all matches sorted by tournament name and then match ID
            if (string.IsNullOrWhiteSpace(input))
            {
                return _allMatches
                    .OrderBy(match => _allTournaments.FirstOrDefault(t => t.MatchLog.GetAllActiveMatches(t.CurrentRound).Any(m => m.Id == match.Id)))
                    .ThenBy(match => match.Id)
                    .Select(match =>
                    {
                        var tournament = _allTournaments.FirstOrDefault(t => t.MatchLog.GetAllActiveMatches(t.CurrentRound).Any(m => m.Id == match.Id));
                        string tournamentName = tournament != null ? tournament.Name : "Unknown Tournament";
                        return new AutocompleteResult($"#{match.Id} | {match.TeamA} vs {match.TeamB} - {tournamentName} ({tournament.TeamSizeFormat} {tournament.GetFormattedTournamentType()})", match.Id);
                    })
                    .ToList();
            }

            // Filter matches based on the input (case-insensitive)
            var matchingMatches = _allMatches
                .Where(match =>
                {
                    var tournament = _allTournaments.FirstOrDefault(t => t.MatchLog.GetAllActiveMatches(t.CurrentRound).Any(m => m.Id == match.Id));
                    //var tournament = _tournamentManager.GetTournamentFromMatchId(match.Id);
                    string tournamentName = tournament != null ? tournament.Name : "Unknown Tournament";
                    return match.Id.Contains(input, StringComparison.OrdinalIgnoreCase) ||
                           match.TeamA.Contains(input, StringComparison.OrdinalIgnoreCase) ||
                           match.TeamB.Contains(input, StringComparison.OrdinalIgnoreCase) ||
                           tournamentName.Contains(input, StringComparison.OrdinalIgnoreCase);
                })
                .OrderBy(match => _tournamentManager.GetTournamentFromMatchId(match.Id)?.Name)
                .ThenBy(match => match.Id)
                .Select(match =>
                {
                    var tournament = _allTournaments.FirstOrDefault(t => t.MatchLog.GetAllActiveMatches(t.CurrentRound).Any(m => m.Id == match.Id));
                    string tournamentName = tournament != null ? tournament.Name : "Unknown Tournament";
                    return new AutocompleteResult($"#{match.Id} | {match.TeamA} vs {match.TeamB} - {tournamentName} ({tournament.TeamSizeFormat} {tournament.GetFormattedTournamentType()})", match.Id);
                })
                .ToList();

            return matchingMatches;
        }

        private List<AutocompleteResult> GetPostMatchIdsMatchingInput(string input)
        {
            var allPostMatches = _matchManager.GetAllPostMatches();
            // If the input is empty or only whitespace, return all post-matches sorted by tournament name and then match ID
            if (string.IsNullOrWhiteSpace(input))
            {
                return allPostMatches
                    .OrderBy(postMatch => _tournamentManager.GetTournamentFromMatchId(postMatch.Id)?.Name)
                    .ThenBy(postMatch => postMatch.Id)
                    .Select(postMatch =>
                    {
                        var tournament = _tournamentManager.GetTournamentFromMatchId(postMatch.Id);
                        string tournamentName = tournament != null ? tournament.Name : "Unknown Tournament";
                        return new AutocompleteResult($"#{postMatch.Id} | {postMatch.Winner} vs {postMatch.Loser} - {tournamentName} ({tournament.TeamSizeFormat} {tournament.GetFormattedTournamentType()})", postMatch.Id);
                    })
                    .ToList();
            }
            // Filter post-matches based on the input (case-insensitive)
            var matchingPostMatches = allPostMatches
                .Where(postMatch =>
                {
                    var tournament = _tournamentManager.GetTournamentFromMatchId(postMatch.Id);
                    string tournamentName = tournament != null ? tournament.Name : "Unknown Tournament";
                    return postMatch.Id.Contains(input, StringComparison.OrdinalIgnoreCase) ||
                           postMatch.Winner.Contains(input, StringComparison.OrdinalIgnoreCase) ||
                           postMatch.Loser.Contains(input, StringComparison.OrdinalIgnoreCase) ||
                           tournamentName.Contains(input, StringComparison.OrdinalIgnoreCase);
                })
                .OrderBy(postMatch => _tournamentManager.GetTournamentFromMatchId(postMatch.Id)?.Name)
                .ThenBy(postMatch => postMatch.Id)
                .Select(postMatch =>
                {
                    var tournament = _tournamentManager.GetTournamentFromMatchId(postMatch.Id);
                    string tournamentName = tournament != null ? tournament.Name : "Unknown Tournament";
                    return new AutocompleteResult($"#{postMatch.Id} | {postMatch.Winner} vs {postMatch.Loser} - {tournamentName} ({tournament.TeamSizeFormat} {tournament.GetFormattedTournamentType()})", postMatch.Id);
                })
                .ToList();
            return matchingPostMatches;
        }

        public List<AutocompleteResult> GetTeamsFromMatchId(string matchId)
        {
            var match = _allMatches.FirstOrDefault(m => m.Id == matchId);
            if (match == null)
            {
                return new List<AutocompleteResult>();
            }
            // Grab teams
            var teamA = _ladderTeams.Concat(_roundRobinTeams).FirstOrDefault(t => t.Name == match.TeamA);
            var teamB = _ladderTeams.Concat(_roundRobinTeams).FirstOrDefault(t => t.Name == match.TeamB);

            // Grab tournament
            var tournament = _allTournaments.FirstOrDefault(t => t.MatchLog.GetAllActiveMatches(t.CurrentRound).Any(m => m.Id == match.Id));

            var results = new List<AutocompleteResult>();
            if (teamA != null)
            {
                results.Add(new AutocompleteResult($"{teamA.Name} - ({tournament.Name} {tournament.TeamSizeFormat} {tournament.GetFormattedTournamentType()})", teamA.Name));
            }
            if (teamB != null)
            {
                results.Add(new AutocompleteResult($"{teamB.Name} - ({tournament.Name} {tournament.TeamSizeFormat} {tournament.GetFormattedTournamentType()})", teamB.Name));
            }
            return results;
        }

        public List<AutocompleteResult> GetTeamsFromPostMatchId(string postMatchId)
        {
            var postMatch = _allPostMatches.FirstOrDefault(pm => pm.Id == postMatchId);
            if (postMatch == null)
            {
                return new List<AutocompleteResult>();
            }
            // Grab teams
            var originalWinner = _ladderTeams.Concat(_roundRobinTeams).FirstOrDefault(t => t.Name == postMatch.Winner);
            var originalLoser = _ladderTeams.Concat(_roundRobinTeams).FirstOrDefault(t => t.Name == postMatch.Loser);

            // Grab tournament
            var tournament = _allTournaments.FirstOrDefault(t => t.MatchLog.GetAllPostMatches().Any(pm => pm.Id == postMatch.Id));

            var results = new List<AutocompleteResult>();
            if (originalWinner != null)
            {
                results.Add(new AutocompleteResult($"{originalWinner.Name} - ({tournament.Name} {tournament.TeamSizeFormat} {tournament.GetFormattedTournamentType()})", originalWinner.Name));
            }
            if (originalLoser != null)
            {
                results.Add(new AutocompleteResult($"{originalLoser.Name} - ({tournament.Name} {tournament.TeamSizeFormat} {tournament.GetFormattedTournamentType()})", originalLoser.Name));
            }
            return results;
        }

        public List<AutocompleteResult> GetLadderTeamNames(string input)
        {
            // Get all teams from all tournaments
            // If the input is empty or only whitespace, return all teams sorted alphabetically
            // Must use the cache data and not call the manager directly
            if (string.IsNullOrWhiteSpace(input))
            {
                return _ladderTeams
                    //.OrderBy(team => _tournamentManager.GetTournamentFromTeamName(team.Name).Name)
                    .OrderBy(team => _ladderTournaments.Where(t => t.Teams.Contains(team)).FirstOrDefault()?.Name)
                    .ThenBy(team => team.Rank)
                    .Select(team => new AutocompleteResult($"#{team.Rank} | {team.Name} - {_ladderTournaments.Where(t => t.Teams.Contains(team)).FirstOrDefault()?.Name} ({_ladderTournaments.Where(t => t.Teams.Contains(team)).FirstOrDefault()?.TeamSizeFormat} {_ladderTournaments.Where(t => t.Teams.Contains(team)).FirstOrDefault()?.GetFormattedTournamentType()})", team.Name))
                    .ToList();
            }
            // Filter teams based on the input (case-insensitive)
            var matchingTeams = _ladderTeams
                .Where(team => team.Name.Contains(input, StringComparison.OrdinalIgnoreCase))
                .OrderBy(team => _ladderTournaments.Where(t => t.Teams.Contains(team)).FirstOrDefault()?.Name)
                .OrderBy(team => team.Rank)
                .Select(team => new AutocompleteResult($"#{team.Rank} | {team.Name} - {_ladderTournaments.Where(t => t.Teams.Contains(team)).FirstOrDefault()?.Name} ({_ladderTournaments.Where(t => t.Teams.Contains(team)).FirstOrDefault()?.TeamSizeFormat} {_ladderTournaments.Where(t => t.Teams.Contains(team)).FirstOrDefault()?.GetFormattedTournamentType()})", team.Name))
                .ToList();
            return matchingTeams;
        }

        public List<AutocompleteResult> GetTeamsForSendChallenge(string input)
        {
            // If the input is empty or only whitespace, return all teams sorted alphabetically
            if (string.IsNullOrWhiteSpace(input))
            {
                return _ladderTeams
                    .Where(team => team.IsChallengeable)
                    .OrderBy(team => team.Rank)
                    .Select(team => new AutocompleteResult($"#{team.Rank} | {team.Name} - {_tournamentManager.GetTournamentFromTeamName(team.Name).Name} ({_tournamentManager.GetTournamentFromTeamName(team.Name).TeamSizeFormat} {_tournamentManager.GetTournamentFromTeamName(team.Name).GetFormattedTournamentType()})", team.Name))
                    .ToList();
            }
            // Filter teams based on the input (case-insensitive)
            var matchingTeams = _ladderTeams
                .Where(team => team.Name.Contains(input, StringComparison.OrdinalIgnoreCase))
                .Where(team => team.IsChallengeable)
                .OrderBy(team => team.Rank)
                .Select(team => new AutocompleteResult($"#{team.Rank} | {team.Name} - {_tournamentManager.GetTournamentFromTeamName(team.Name).Name} ({_tournamentManager.GetTournamentFromTeamName(team.Name).TeamSizeFormat} {_tournamentManager.GetTournamentFromTeamName(team.Name).GetFormattedTournamentType()})", team.Name))
                .ToList();
            return matchingTeams;
        }

        public List<AutocompleteResult> GetTeamsForCancelChallenge(string input)
        {
            // Filter to only teams that are a Challenger in a challenge
            var challengerTeams = _ladderTeams;

            // If the input is empty or only whitespace, return all challenger teams sorted alphabetically
            if (string.IsNullOrWhiteSpace(input))
            {
                return challengerTeams
                    .OrderBy(team => team.Rank)
                    .Select(team => new AutocompleteResult($"#{team.Rank} | {team.Name} - {_tournamentManager.GetTournamentFromTeamName(team.Name).Name} ({_tournamentManager.GetTournamentFromTeamName(team.Name).TeamSizeFormat} {_tournamentManager.GetTournamentFromTeamName(team.Name).GetFormattedTournamentType()})", team.Name))
                    .ToList();
            }
            // Filter challenger teams based on the input (case-insensitive)
            var matchingTeams = challengerTeams
                .Where(team => team.Name.Contains(input, StringComparison.OrdinalIgnoreCase))
                .OrderBy(team => team.Rank)
                .Select(team => new AutocompleteResult($"#{team.Rank} | {team.Name} - {_tournamentManager.GetTournamentFromTeamName(team.Name).Name} ({_tournamentManager.GetTournamentFromTeamName(team.Name).TeamSizeFormat} {_tournamentManager.GetTournamentFromTeamName(team.Name).GetFormattedTournamentType()})", team.Name))
                .ToList();
            return matchingTeams;
        }

        public List<AutocompleteResult> GetTournamentIdsMatchingInput(string input)
        {
            // If the input is empty or only whitespace, return all tournaments sorted alphabetically
            if (string.IsNullOrWhiteSpace(input))
            {
                // Return all tournaments, sorted alphabetically by name
                return _allTournaments
                    .OrderBy(tournament => tournament.Name)
                    .Take(25)
                    .Select(tournament => new AutocompleteResult($"{tournament.Name} - ({tournament.TeamSizeFormat} {tournament.GetFormattedTournamentType()})", tournament.Id))
                    .ToList();
            }

            // Filter tournaments based on the input (case-insensitive)
            var matchingTournaments = _allTournaments
                .Where(tournament => tournament.Name.Contains(input, StringComparison.OrdinalIgnoreCase))
                .OrderBy(tournament => tournament.Name)
                .Take(25)
                .Select(tournament => new AutocompleteResult($"{tournament.Name} - ({tournament.TeamSizeFormat} {tournament.GetFormattedTournamentType()})", tournament.Id))
                .ToList();

            return matchingTournaments;
        }

        public List<AutocompleteResult> GetRoundRobinTournamentIds(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                // Return all RR tournaments, sorted alphabetically by name
                return _roundRobinTournaments
                    .OrderBy(tournament => tournament.Name)
                    .Select(tournament => new AutocompleteResult($"{tournament.Name} - ({tournament.TeamSizeFormat} {tournament.GetFormattedTournamentType()})", tournament.Id))
                    .ToList();
            }
            // Filter RR tournaments based on the input (case-insensitive)
            var matchingTournaments = _roundRobinTournaments
                .Where(tournament => tournament.Name.Contains(input, StringComparison.OrdinalIgnoreCase))
                .OrderBy(tournament => tournament.Name)
                .Select(tournament => new AutocompleteResult($"{tournament.Name} - ({tournament.TeamSizeFormat} {tournament.GetFormattedTournamentType()})", tournament.Id))
                .ToList();
            return matchingTournaments;
        }

        public List<AutocompleteResult> GetRoundBasedTournamentIds(string input)
        {
            // Get all normal RR tournaments
            // TODO: Extend this to include elimination tournaments when needed
            var tournaments = _allTournaments.Where(t => t.Type.Equals(TournamentType.RoundRobin) && t.RoundRobinMatchType.Equals(RoundRobinMatchType.Normal));

            // If the input is empty or only whitespace, return all round-based tournaments sorted alphabetically
            if (string.IsNullOrWhiteSpace(input))
            {
                // Return all round-based tournaments, sorted alphabetically by name
                return tournaments
                    .OrderBy(tournament => tournament.Name)
                    .Select(tournament => new AutocompleteResult($"{tournament.Name} - ({tournament.TeamSizeFormat} {tournament.GetFormattedTournamentType()})", tournament.Id))
                    .ToList();
            }
            // Filter round-based tournaments based on the input (case-insensitive)
            var matchingTournaments = tournaments
                .Where(tournament => tournament.Name.Contains(input, StringComparison.OrdinalIgnoreCase))
                .OrderBy(tournament => tournament.Name)
                .Select(tournament => new AutocompleteResult($"{tournament.Name} - ({tournament.TeamSizeFormat} {tournament.GetFormattedTournamentType()})", tournament.Id))
                .ToList();
            return matchingTournaments;
        }
    }
}
