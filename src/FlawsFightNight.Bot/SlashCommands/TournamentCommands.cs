using Discord;
using Discord.Interactions;
using FlawsFightNight.Bot.Autocomplete;
using FlawsFightNight.Bot.Modals;
using FlawsFightNight.Bot.PreconditionAttributes;
using FlawsFightNight.CommandsLogic.TeamCommands;
using FlawsFightNight.CommandsLogic.TournamentCommands;
using FlawsFightNight.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Bot.SlashCommands
{
    [Group("tournament", "Commands related to tournaments like creating, removal, etc.")]
    public class TournamentCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private AutocompleteCache _autocompleteCache;
        private CreateTournamentLogic _createTournamentLogic;
        private LockInRoundLogic _lockInRoundLogic;
        private LockTeamsLogic _lockTeamsLogic;
        private NextRoundLogic _nextRoundLogic;
        private SetupRoundRobinTournamentLogic _setupTournamentLogic;
        private ShowAllTournamentsLogic _showAllTournamentsLogic;
        private UnlockRoundLogic _unlockRoundLogic;
        private UnlockTeamsLogic _unlockTeamsLogic;

        public TournamentCommands(AutocompleteCache autocompleteCache, CreateTournamentLogic createTournamentLogic, LockInRoundLogic lockInRoundLogic, LockTeamsLogic lockTeamsLogic, NextRoundLogic nextRoundLogic, SetupRoundRobinTournamentLogic setupTournamentLogic, ShowAllTournamentsLogic showAllTournamentsLogic, UnlockRoundLogic unlockRoundLogic, UnlockTeamsLogic unlockTeamsLogic)
        {
            _autocompleteCache = autocompleteCache;
            _createTournamentLogic = createTournamentLogic;
            _lockInRoundLogic = lockInRoundLogic;
            _lockTeamsLogic = lockTeamsLogic;
            _nextRoundLogic = nextRoundLogic;
            _setupTournamentLogic = setupTournamentLogic;
            _showAllTournamentsLogic = showAllTournamentsLogic;
            _unlockRoundLogic = unlockRoundLogic;
            _unlockTeamsLogic = unlockTeamsLogic;
        }

        [SlashCommand("create", "Create a new tournament")]
        [RequireGuildAdmin]
        public async Task CreateTournamentAsync(
            [Summary("name", "The name of the tournament")] string name,
            [Summary("type", "The type of the tournament")] TournamentType tournamentType,
            [Summary("team_size", "The size of each team in the tournament")] int teamSize,
            [Summary("description", "A description of the tournament (optional)")] string? description = null)
        {
            try
            {
                await DeferAsync();
                var result = _createTournamentLogic.CreateTournamentProcess(Context, name, tournamentType, teamSize, description);
                await FollowupAsync(embed: result);

                // TODO Re-enable when correcting autocomplete
                //_autocompleteCache.UpdateCache();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Command Error: {ex}");
                await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
            }
        }

        [SlashCommand("delete", "Delete a tournament")]
        [RequireGuildAdmin]
        public async Task DeleteTournamentAsync()
        {
            try
            {
                await RespondWithModalAsync<DeleteTournamentModal>("delete_tournament");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Command Error: {ex}");
                await RespondAsync("An error occurred while processing this command.", ephemeral: true);
            }
        }

        [SlashCommand("lock-teams", "Lock teams in a tournament")]
        [RequireGuildAdmin]
        public async Task LockTeamsAsync(
            [Summary("tournament_id", "The ID of the tournament to lock teams in")
            //, Autocomplete(typeof(RoundRobinTournamentIdAutocomplete))
            ] string tournamentId)
        {
            try
            {
                await DeferAsync();
                var result = _lockTeamsLogic.LockTeamsProcess(tournamentId);
                await FollowupAsync(embed: result);
                // TODO Re-enable when correcting autocomplete
                //_autocompleteCache.UpdateCache();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Command Error: {ex}");
                await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
            }
        }

        [SlashCommand("unlock-teams", "Unlock teams in a tournament")]
        [RequireGuildAdmin]
        public async Task UnlockTeamsAsync(
            [Summary("tournament_id", "The ID of the tournament to unlock teams in")
            //, Autocomplete(typeof(RoundRobinTournamentIdAutocomplete))
            ] string tournamentId)
        {
            try
            {
                await DeferAsync();
                var result = _unlockTeamsLogic.UnlockTeamsProcess(tournamentId);
                await FollowupAsync(embed: result);
                // TODO Re-enable when correcting autocomplete
                //_autocompleteCache.UpdateCache();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Command Error: {ex}");
                await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
            }
        }

        [SlashCommand("start", "Start a tournament")]
        [RequireGuildAdmin]
        public async Task StartTournamentAsync()
        {
            try
            {
                await RespondWithModalAsync<StartTournamentModal>("start_tournament");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Command Error: {ex}");
                await RespondAsync("An error occurred while processing this command.", ephemeral: true);
            }
        }

        [SlashCommand("setup_round_robin", "Setup a RR tournaments rules and habits before starting it.")]
        [RequireGuildAdmin]
        public async Task SetupTournamentAsync(
            [Summary("tournament_id", "The ID of the tournament to setup")
            //, Autocomplete(typeof(RoundRobinTournamentIdAutocomplete))
            ] string tournamentId,
            [Summary("match_type", "Normal for round based, open for any time matches")] RoundRobinMatchType matchType,
            [Summary("tie_breaker_ruleset", "The ruleset to use for tie breakers")] TieBreakerType tieBreakerType,
            [Summary("length", "Whether the tournament is a double or single round robin")] RoundRobinLengthType roundRobinType)
        {
            try
            {
                await DeferAsync();
                var result = _setupTournamentLogic.SetupRoundRobinTournamentProcess(tournamentId, matchType, tieBreakerType, roundRobinType);
                await FollowupAsync(embed: result);
                // TODO Re-enable when correcting autocomplete
                //_autocompleteCache.UpdateCache();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Command Error: {ex}");
                await RespondAsync("An error occurred while processing this command.", ephemeral: true);
            }
        }

        [SlashCommand("end", "End a tournament")]
        [RequireGuildAdmin]
        public async Task EndTournamentAsync()
        {
            try
            {
                await RespondWithModalAsync<EndTournamentModal>("end_tournament");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Command Error: {ex}");
                await RespondAsync("An error occurred while processing this command.", ephemeral: true);
            }
        }

        [SlashCommand("lock-in-round", "Lock in round results after all matches played (Normal RR & Elim)")]
        [RequireGuildAdmin]
        public async Task LockInRoundAsync(
            [Summary("tournament_id", "The ID of the tournament to round lock")
            //, Autocomplete(typeof(RoundBasedTournamentIdAutocomplete))
            ] string tournamentId)
        {
            try
            {
                await DeferAsync();
                var result = _lockInRoundLogic.LockInRoundProcess(tournamentId);
                await FollowupAsync(embed: result);
                // TODO Re-enable when correcting autocomplete
                //_autocompleteCache.UpdateCache();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Command Error: {ex}");
                await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
            }
        }

        [SlashCommand("unlock-round", "Unlock the current round to make changes if needed")]
        [RequireGuildAdmin]
        public async Task UnlockRoundAsync(
            [Summary("tournament_id", "The ID of the tournament to unlock the round")
            //, Autocomplete(typeof(RoundBasedTournamentIdAutocomplete))
            ] string tournamentId)
        {
            try
            {
                await DeferAsync();
                var result = _unlockRoundLogic.UnlockRoundProcess(tournamentId);
                await FollowupAsync(embed: result);
                // TODO Re-enable when correcting autocomplete
                //_autocompleteCache.UpdateCache();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Command Error: {ex}");
                await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
            }
        }

        [SlashCommand("next-round", "Advance to the next round of certain tournaments if conditions are met.")]
        [RequireGuildAdmin]
        public async Task NextRoundAsync(
            [Summary("tournament_id", "The ID of the tournament to advance the round")
            //, Autocomplete(typeof(RoundBasedTournamentIdAutocomplete))
            ] string tournamentId)
        {
            try
            {
                await DeferAsync();
                var result = _nextRoundLogic.NextRoundProcess(tournamentId);
                await FollowupAsync(embed: result);
                // TODO Re-enable when correcting autocomplete
                //_autocompleteCache.UpdateCache();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Command Error: {ex}");
                await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
            }
        }

        [SlashCommand("show-all", "Get information on every tournament on file right now.")]
        public async Task ShowAllTournamentsAsync()
        {
            try
            {
                await DeferAsync();
                var result = _showAllTournamentsLogic.ShowAllTournamentsProcess();
                await FollowupAsync(embed: result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Command Error: {ex}");
                await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
            }
        }
    }
}
