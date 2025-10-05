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
        private TeamManager _teamManager;
        private TournamentManager _tournamentManager;

        public AutocompleteResolver(IServiceProvider services, TeamManager teamManager, TournamentManager tournamentManager)
        {
            _services = services;

            // Initialize Managers here
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
                if (HasAutocomplete(interaction.Data.CommandName))
                {
                    var focusedOption = interaction.Data.Current;
                    if (focusedOption != null)
                    {
                        // Get the current input value
                        string input = focusedOption.Value?.ToString()?.Trim() ?? string.Empty;

                        List<AutocompleteResult> suggestions = focusedOption.Name switch
                        {
                            "tournament_id" => string.IsNullOrWhiteSpace(input)
                                ? GetTournamentIdsMatchingInput("")
                                : GetTournamentIdsMatchingInput(input),
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
                "team",
                // Add more command names as needed
            };
            Console.WriteLine($"{commandsWithAutocomplete.Contains(commandName)}");
            return commandsWithAutocomplete.Contains(commandName);
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
