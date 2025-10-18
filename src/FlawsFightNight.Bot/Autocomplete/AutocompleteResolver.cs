using Discord;
using Discord.WebSocket;
using FlawsFightNight.Core.Enums;
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
                // quick sanity check — don’t even start heavy logic if no matching autocomplete
                if (!HasAutocomplete(interaction.Data.CommandName))
                    return;

                var focusedOption = interaction.Data.Current;
                if (focusedOption == null)
                    return;

                string input = focusedOption.Value?.ToString()?.Trim() ?? string.Empty;
                List<AutocompleteResult> suggestions = new();

                // run heavy logic in a background task to avoid blocking the gateway
                var task = Task.Run(() =>
                {
                    switch (focusedOption.Name)
                    {
                        case "challenger_team":
                            return string.IsNullOrWhiteSpace(input)
                                ? GetTeamsForCancelChallenge("")
                                : GetTeamsForCancelChallenge(input);

                        case "challenger_team_name":
                            return string.IsNullOrWhiteSpace(input)
                                ? GetTeamsForSendChallenge("")
                                : GetTeamsForSendChallenge(input);

                        case "challenged_team":
                            var challengerTeamName =
                                interaction.Data.Options.FirstOrDefault(o => o.Name == "challenger_team_name")?.Value as string;
                            if (string.IsNullOrWhiteSpace(challengerTeamName))
                                return new List<AutocompleteResult>();

                            return string.IsNullOrWhiteSpace(input)
                                ? GetTeamsForSendChallenge("").Where(t => !Equals(t.Value, challengerTeamName)).ToList()
                                : GetTeamsForSendChallenge(input).Where(t => !Equals(t.Value, challengerTeamName)).ToList();

                        case "ladder_team_name":
                            return string.IsNullOrWhiteSpace(input)
                                ? GetLadderTeamsFromName("")
                                : GetLadderTeamsFromName(input);

                        case "match_id":
                            return string.IsNullOrWhiteSpace(input)
                                ? GetMatchIdsMatchingInput("")
                                : GetMatchIdsMatchingInput(input);

                        case "tournament_id":
                            return string.IsNullOrWhiteSpace(input)
                                ? GetTournamentIdsMatchingInput("")
                                : GetTournamentIdsMatchingInput(input);

                        case "post_match_id":
                            return string.IsNullOrWhiteSpace(input)
                                ? GetPostMatchIdsMatchingInput("")
                                : GetPostMatchIdsMatchingInput(input);

                        case "r_tournament_id":
                            return string.IsNullOrWhiteSpace(input)
                                ? GetRoundBasedTournamentIdsMatchingInput("")
                                : GetRoundBasedTournamentIdsMatchingInput(input);

                        case "rr_tournament_id":
                            return string.IsNullOrWhiteSpace(input)
                                ? GetRoundRobinTournamentIdsMatchingInput("")
                                : GetRoundRobinTournamentIdsMatchingInput(input);

                        case "winning_team_name":
                            var matchId = interaction.Data.Options.FirstOrDefault(o => o.Name == "match_id")?.Value as string;
                            var postMatchId = interaction.Data.Options.FirstOrDefault(o => o.Name == "post_match_id")?.Value as string;

                            if (!string.IsNullOrWhiteSpace(matchId))
                                return GetTeamsFromMatchId(matchId)
                                    .Where(t => string.IsNullOrWhiteSpace(input) ||
                                                t.Name.Contains(input, StringComparison.OrdinalIgnoreCase))
                                    .ToList();

                            if (!string.IsNullOrWhiteSpace(postMatchId))
                                return GetTeamsFromPostMatchId(postMatchId)
                                    .Where(t => string.IsNullOrWhiteSpace(input) ||
                                                t.Name.Contains(input, StringComparison.OrdinalIgnoreCase))
                                    .ToList();

                            return new List<AutocompleteResult>();

                        default:
                            return new List<AutocompleteResult>();
                    }
                });

                // timeout guard — only wait up to 2.5 seconds
                if (await Task.WhenAny(task, Task.Delay(2500)) == task)
                {
                    suggestions = task.Result ?? new();
                }
                else
                {
                    Console.WriteLine("[Autocomplete] Timeout - defaulting to empty suggestions");
                    suggestions = new();
                }

                // respond safely — only once
                await interaction.RespondAsync(suggestions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Autocomplete] Exception: {ex}");
                try
                {
                    // ensure Discord receives *something* so it doesn’t hang
                    await interaction.RespondAsync(new List<AutocompleteResult>());
                }
                catch { /* ignore double response */ }
            }
        }


        private bool HasAutocomplete(string commandName)
        {
            // List of commands that have autocomplete functionality
            var commandsWithAutocomplete = new HashSet<string>
            {
                "match",
                "settings",
                "team",
                "tournament"
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

        private List<AutocompleteResult> GetLadderTeamsFromName(string input)
        {
            // Get all teams from all tournaments
            var allTeams = _teamManager.GetAllLadderTeams();
            // If the input is empty or only whitespace, return all teams sorted alphabetically
            if (string.IsNullOrWhiteSpace(input))
            {
                return allTeams
                    .OrderBy(team => team.Rank)
                    .OrderBy(team => _tournamentManager.GetTournamentFromTeamName(team.Name).Name)
                    .Select(team => new AutocompleteResult($"#{team.Rank} | {team.Name} - {_tournamentManager.GetTournamentFromTeamName(team.Name).Name} ({_tournamentManager.GetTournamentFromTeamName(team.Name).TeamSizeFormat} {_tournamentManager.GetTournamentFromTeamName(team.Name).GetFormattedTournamentType()})", team.Name))
                    .ToList();
            }
            // Filter teams based on the input (case-insensitive)
            var matchingTeams = allTeams
                .Where(team => team.Name.Contains(input, StringComparison.OrdinalIgnoreCase))
                .OrderBy(team => _tournamentManager.GetTournamentFromTeamName(team.Name).Name)
                .OrderBy(team => team.Rank)
                .Select(team => new AutocompleteResult($"#{team.Rank} | {team.Name} - {_tournamentManager.GetTournamentFromTeamName(team.Name).Name} ({_tournamentManager.GetTournamentFromTeamName(team.Name).TeamSizeFormat} {_tournamentManager.GetTournamentFromTeamName(team.Name).GetFormattedTournamentType()})", team.Name))
                .ToList();
            return matchingTeams;
        }

        private List<AutocompleteResult> GetTeamsForSendChallenge(string input)
        {
            // Get all teams from all tournaments
            var allTeams = _teamManager.GetAllLadderTeams();
            // If the input is empty or only whitespace, return all teams sorted alphabetically
            if (string.IsNullOrWhiteSpace(input))
            {
                return allTeams
                    .Where(team => team.IsChallengeable)
                    .OrderBy(team => team.Rank)
                    .Select(team => new AutocompleteResult($"#{team.Rank} | {team.Name} - {_tournamentManager.GetTournamentFromTeamName(team.Name).Name} ({_tournamentManager.GetTournamentFromTeamName(team.Name).TeamSizeFormat} {_tournamentManager.GetTournamentFromTeamName(team.Name).GetFormattedTournamentType()})", team.Name))
                    .ToList();
            }
            // Filter teams based on the input (case-insensitive)
            var matchingTeams = allTeams
                .Where(team => team.Name.Contains(input, StringComparison.OrdinalIgnoreCase))
                .Where(team => team.IsChallengeable)
                .OrderBy(team => team.Rank)
                .Select(team => new AutocompleteResult($"#{team.Rank} | {team.Name} - {_tournamentManager.GetTournamentFromTeamName(team.Name).Name} ({_tournamentManager.GetTournamentFromTeamName(team.Name).TeamSizeFormat} {_tournamentManager.GetTournamentFromTeamName(team.Name).GetFormattedTournamentType()})", team.Name))
                .ToList();
            return matchingTeams;
        }

        private List<AutocompleteResult> GetTeamsForCancelChallenge(string input)
        {
            // Get all teams from all tournaments
            var allTeams = _teamManager.GetAllLadderTeams();

            // Filter to only teams that are a Challenger in a challenge
            var challengerTeams = _matchManager.GetAllChallengerTeams(allTeams);

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

        private List<AutocompleteResult> GetRoundRobinTournamentIdsMatchingInput(string input)
        {
            // Get all RR tournaments
            var tournaments = _tournamentManager.GetAllTournaments().Where(t => t.Type.Equals(TournamentType.RoundRobin));
            // If the input is empty or only whitespace, return all RR tournaments sorted alphabetically
            if (string.IsNullOrWhiteSpace(input))
            {
                // Return all RR tournaments, sorted alphabetically by name
                return tournaments
                    .OrderBy(tournament => tournament.Name)
                    .Select(tournament => new AutocompleteResult($"{tournament.Name} - ({tournament.TeamSizeFormat} {tournament.GetFormattedTournamentType()})", tournament.Id))
                    .ToList();
            }
            // Filter RR tournaments based on the input (case-insensitive)
            var matchingTournaments = tournaments
                .Where(tournament => tournament.Name.Contains(input, StringComparison.OrdinalIgnoreCase))
                .OrderBy(tournament => tournament.Name)
                .Select(tournament => new AutocompleteResult($"{tournament.Name} - ({tournament.TeamSizeFormat} {tournament.GetFormattedTournamentType()})", tournament.Id))
                .ToList();
            return matchingTournaments;
        }

        private List<AutocompleteResult> GetRoundBasedTournamentIdsMatchingInput(string input)
        {
            // Get all normal RR tournaments
            var tournaments = _tournamentManager.GetAllTournaments().Where(t => t.Type.Equals(TournamentType.RoundRobin) && t.RoundRobinMatchType.Equals(RoundRobinMatchType.Normal));
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
