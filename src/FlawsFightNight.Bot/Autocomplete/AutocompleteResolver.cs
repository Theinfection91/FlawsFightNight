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

                        List<AutocompleteResult> suggestions = focusedOption.Name switch
                        {
                            "match_id" => string.IsNullOrWhiteSpace(input)
                                ? GetMatchIdsMatchingInput("")
                                : GetMatchIdsMatchingInput(input),
                            "tournament_id" => string.IsNullOrWhiteSpace(input)
                                ? GetTournamentIdsMatchingInput("")
                                : GetTournamentIdsMatchingInput(input),
                            "winning_team_name" => string.IsNullOrWhiteSpace(input) && interaction.Data.Options.FirstOrDefault(o => o.Name == "match_id")?.Value is string matchId && !string.IsNullOrWhiteSpace(matchId)
                                ? GetTeamsFromMatchId(matchId) 
                                : !string.IsNullOrWhiteSpace(input) && interaction.Data.Options.FirstOrDefault(o => o.Name == "match_id")?.Value is string matchId2 && !string.IsNullOrWhiteSpace(matchId2)
                                ? GetTeamsFromMatchId(matchId2).Where(t => t.Name.Contains(input, StringComparison.OrdinalIgnoreCase)).ToList()
                                : new List<AutocompleteResult>(),
                            _ => new List<AutocompleteResult>()
                        };
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
                        return new AutocompleteResult($"{tournamentName} - {match.Id} ({match.TeamA} vs {match.TeamB})", match.Id);
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
                    return new AutocompleteResult($"{tournamentName} - {match.Id} ({match.TeamA} vs {match.TeamB})", match.Id);
                })
                .ToList();

            return matchingMatches;
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
                    .Select(tournament => new AutocompleteResult($"{tournament.Name} - ({tournament.TeamSizeFormat} {tournament.GetFormattedTournamentType()})", tournament.Name))
                    .ToList();
            }

            // Filter tournaments based on the input (case-insensitive)
            var matchingTournaments = tournaments
                .Where(tournament => tournament.Name.Contains(input, StringComparison.OrdinalIgnoreCase))
                .OrderBy(tournament => tournament.Name)
                .Select(tournament => new AutocompleteResult($"{tournament.Name} - ({tournament.TeamSizeFormat} {tournament.GetFormattedTournamentType()})", tournament.Name))
                .ToList();

            return matchingTournaments;
        }
    }
}
