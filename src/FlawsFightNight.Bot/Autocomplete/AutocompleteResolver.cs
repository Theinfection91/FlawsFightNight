using Discord;
using Discord.WebSocket;
using FlawsFightNight.Managers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Bot.Autocomplete
{
    public class AutocompleteResolver
    {
        private readonly IServiceProvider _services;

        // Add Managers as needed here
        private MatchManager _matchManager;
        private TeamManager _teamManager;
        private TournamentManager _tournamentManager;

        public AutocompleteResolver(IServiceProvider services, MatchManager matchManager, TeamManager teamManager, TournamentManager tournamentManager)
        {
            _services = services;

            // Initialize Managers here
            _matchManager = matchManager;
            _teamManager = teamManager;
            _tournamentManager = tournamentManager;
        }

        public Task InitializeAsync()
        {
            var client = _services.GetRequiredService<DiscordSocketClient>();
            client.AutocompleteExecuted += HandleAutoCompleteAsync;
            return Task.CompletedTask;
        }

        private async Task HandleAutoCompleteAsync(SocketAutocompleteInteraction interaction)
        {
            try
            {
                Console.WriteLine($"[Autocomplete] Command: {interaction.Data.CommandName}, Option: {interaction.Data.Current?.Name}, Value: {interaction.Data.Current?.Value}");
                if (HasAutocomplete(interaction.Data.CommandName))
                {
                    var focusedOption = interaction.Data.Current;
                    if (focusedOption != null)
                    {
                        // Get the current input value
                        string input = focusedOption.Value?.ToString()?.Trim() ?? string.Empty;

                        List<AutocompleteResult> suggestions;
                        switch (focusedOption.Name)
                        {
                            case "match_id":
                                suggestions = string.IsNullOrWhiteSpace(input)
                                    ? GetMatchIdsMatchingInput("")
                                    : GetMatchIdsMatchingInput(input);
                                break;
                            case "tournament_id":
                                suggestions = string.IsNullOrWhiteSpace(input)
                                    ? GetTournamentIdsMatchingInput("")
                                    : GetTournamentIdsMatchingInput(input);
                                break;
                            case "post_match_id":
                                suggestions = string.IsNullOrWhiteSpace(input)
                                    ? GetPostMatchIdsMatchingInput("")
                                    : GetPostMatchIdsMatchingInput(input);
                                break;
                            case "winning_team_name":
                                if (string.IsNullOrWhiteSpace(input) && interaction.Data.Options.FirstOrDefault(o => o.Name == "match_id")?.Value is string matchId && !string.IsNullOrWhiteSpace(matchId))
                                {
                                    suggestions = GetTeamsFromMatchId(matchId);
                                }
                                else if (!string.IsNullOrWhiteSpace(input) && interaction.Data.Options.FirstOrDefault(o => o.Name == "match_id")?.Value is string matchId2 && !string.IsNullOrWhiteSpace(matchId2))
                                {
                                    suggestions = GetTeamsFromMatchId(matchId2).Where(t => t.Name.Contains(input, StringComparison.OrdinalIgnoreCase)).ToList();
                                }
                                else if (string.IsNullOrWhiteSpace(input) && interaction.Data.Options.FirstOrDefault(o => o.Name == "post_match_id")?.Value is string postMatchId && !string.IsNullOrWhiteSpace(postMatchId))
                                {
                                    suggestions = GetTeamsFromPostMatchId(postMatchId);
                                }
                                else if (!string.IsNullOrWhiteSpace(input) && interaction.Data.Options.FirstOrDefault(o => o.Name == "post_match_id")?.Value is string postMatchId2 && !string.IsNullOrWhiteSpace(postMatchId2))
                                {
                                    suggestions = GetTeamsFromPostMatchId(postMatchId2).Where(t => t.Name.Contains(input, StringComparison.OrdinalIgnoreCase)).ToList();
                                }
                                else
                                {
                                    suggestions = new List<AutocompleteResult>();
                                }
                                break;
                            default:
                                suggestions = new List<AutocompleteResult>();
                                break;
                        }
                        await interaction.RespondAsync(suggestions);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Autocomplete] Exception: {ex}");
            }
        }

        private bool HasAutocomplete(string commandName)
        {
            // List of commands that have autocomplete functionality
            var commandsWithAutocomplete = new HashSet<string>
            {
                "match",
                "team",
                // Add more command names as needed
            };
            return commandsWithAutocomplete.Contains(commandName);
        }

        private List<AutocompleteResult> GetMatchIdsMatchingInput(string input)
        {
            var allMatches = _matchManager.GetAllActiveMatches();

            // If the input is empty or only whitespace, return all matches sorted by tournament name and then match ID
            if (string.IsNullOrWhiteSpace(input))
            {
                return allMatches
                    .OrderBy(match => _tournamentManager.GetTournamentFromMatchId(match.Id)?.Name)
                    .ThenBy(match => match.Id)
                    .Select(match =>
                    {
                        var tournament = _tournamentManager.GetTournamentFromMatchId(match.Id);
                        string tournamentName = tournament != null ? tournament.Name : "Unknown Tournament";
                        return new AutocompleteResult($"#{match.Id} | {match.TeamA} vs {match.TeamB} - {tournamentName} ({tournament.TeamSizeFormat} {tournament.GetFormattedTournamentType()})", match.Id);
                    })
                    .ToList();
            }

            // Filter matches based on the input (case-insensitive)
            var matchingMatches = allMatches
                .Where(match =>
                {
                    var tournament = _tournamentManager.GetTournamentFromMatchId(match.Id);
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
                    var tournament = _tournamentManager.GetTournamentFromMatchId(match.Id);
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

        private List<AutocompleteResult> GetTeamsFromMatchId(string matchId)
        {
            var match = _matchManager.GetMatchFromDatabase(matchId);
            if (match == null)
            {
                return new List<AutocompleteResult>();
            }
            // Grab teams
            var teamA = _teamManager.GetTeamByName(match.TeamA);
            var teamB = _teamManager.GetTeamByName(match.TeamB);

            // Grab tournament
            var tournament = _tournamentManager.GetTournamentFromMatchId(matchId);

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

        private List<AutocompleteResult> GetTeamsFromPostMatchId(string postMatchId)
        {
            var match = _matchManager.GetPostMatchById(postMatchId);
            if (match == null)
            {
                return new List<AutocompleteResult>();
            }
            // Grab teams
            var originalWinner = _teamManager.GetTeamByName(match.Winner);
            var originalLoser = _teamManager.GetTeamByName(match.Loser);
            // Grab tournament
            var tournament = _tournamentManager.GetTournamentFromMatchId(postMatchId);
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

        private List<AutocompleteResult> GetTournamentIdsMatchingInput(string input)
        {
            // Get all tournaments
            var tournaments = _tournamentManager.GetAllTournaments();

            // If the input is empty or only whitespace, return all tournaments sorted alphabetically
            if (string.IsNullOrWhiteSpace(input))
            {
                // Return all tournaments, sorted alphabetically by name
                return tournaments
                    .OrderBy(tournament => tournament.Name)
                    .Select(tournament => new AutocompleteResult($"{tournament.Name} - ({tournament.TeamSizeFormat} {tournament.GetFormattedTournamentType()})", tournament.Id))
                    .ToList();
            }

            // Filter tournaments based on the input (case-insensitive)
            var matchingTournaments = tournaments
                .Where(tournament => tournament.Name.Contains(input, StringComparison.OrdinalIgnoreCase))
                .OrderBy(tournament => tournament.Name)
                .Select(tournament => new AutocompleteResult($"{tournament.Name} - ({tournament.TeamSizeFormat} {tournament.GetFormattedTournamentType()})", tournament.Id))
                .ToList();

            return matchingTournaments;
        }
    }
}
