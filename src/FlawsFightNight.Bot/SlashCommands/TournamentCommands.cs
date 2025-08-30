using Discord;
using Discord.Interactions;
using FlawsFightNight.Bot.Modals;
using FlawsFightNight.Bot.PreconditionAttributes;
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
        private CreateTournamentLogic _createTournamentLogic;
        private EndTournamentLogic _endTournamentLogic;
        private LockInRoundLogic _lockInRoundLogic;
        private LockTeamsLogic _lockTeamsLogic;
        private NextRoundLogic _nextRoundLogic;
        private SetupTournamentLogic _setupTournamentLogic;
        private StartTournamentLogic _startTournamentLogic;
        private UnlockRoundLogic _unlockRoundLogic;
        private UnlockTeamsLogic _unlockTeamsLogic;

        public TournamentCommands(CreateTournamentLogic createTournamentLogic, EndTournamentLogic endTournamentLogic, LockInRoundLogic lockInRoundLogic, LockTeamsLogic lockTeamsLogic, NextRoundLogic nextRoundLogic, SetupTournamentLogic setupTournamentLogic, StartTournamentLogic startTournamentLogic, UnlockRoundLogic unlockRoundLogic, UnlockTeamsLogic unlockTeamsLogic)
        {
            _createTournamentLogic = createTournamentLogic;
            _endTournamentLogic = endTournamentLogic;
            _lockInRoundLogic = lockInRoundLogic;
            _lockTeamsLogic = lockTeamsLogic;
            _nextRoundLogic = nextRoundLogic;
            _setupTournamentLogic = setupTournamentLogic;
            _startTournamentLogic = startTournamentLogic;
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
                var result = _createTournamentLogic.CreateTournamentProcess(Context, name, tournamentType, teamSize, description);
                await RespondAsync(embed: result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Command Error: {ex}");
                await RespondAsync("An error occurred while processing this command.", ephemeral: true);
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
            [Summary("tournament_id", "The ID of the tournament to lock teams in")] string tournamentId)
        {
            try
            {
                var result = _lockTeamsLogic.LockTeamsProcess(Context, tournamentId);
                await RespondAsync(embed: result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Command Error: {ex}");
                await RespondAsync("An error occurred while processing this command.", ephemeral: true);
            }
        }

        [SlashCommand("unlock-teams", "Unlock teams in a tournament")]
        [RequireGuildAdmin]
        public async Task UnlockTeamsAsync(
            [Summary("tournament_id", "The ID of the tournament to unlock teams in")] string tournamentId)
        {
            try
            {
                var result = _unlockTeamsLogic.UnlockTeamsProcess(tournamentId);
                await RespondAsync(embed: result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Command Error: {ex}");
                await RespondAsync("An error occurred while processing this command.", ephemeral: true);
            }
        }

        [SlashCommand("start", "Start a tournament")]
        [RequireGuildAdmin]
        public async Task StartTournamentAsync(
            [Summary("tournament_id", "The ID of the tournament to start")] string tournamentId)
        {
            try
            {
                var result = _startTournamentLogic.StartTournamentProcess(tournamentId);
                await RespondAsync(embed: result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Command Error: {ex}");
                await RespondAsync("An error occurred while processing this command.", ephemeral: true);
            }
        }

        [SlashCommand("setup", "Setup a tournaments rules and habits before starting it.")]
        [RequireGuildAdmin]
        public async Task SetupTournamentAsync(
            [Summary("tournament_id", "The ID of the tournament to setup")] string tournamentId,
            [Summary("tie_breaker_ruleset", "The ruleset to use for tie breakers")] TieBreakerType tieBreakerType,
            [Summary("is_double_round_robin", "Whether the tournament is a double round robin (only for Round Robin type)")] RoundRobinType roundRobinType)
        {
            try
            {
                var result = _setupTournamentLogic.SetupTournamentProcess(tournamentId, tieBreakerType, roundRobinType);
                await RespondAsync(embed: result);
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
                //var result = _endTournamentLogic.EndTournamentProcess(tournamentId);
                //await RespondAsync(embed: result);
                await RespondWithModalAsync<EndTournamentModal>("end_tournament");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Command Error: {ex}");
                await RespondAsync("An error occurred while processing this command.", ephemeral: true);
            }
        }

        [SlashCommand("lock-in-round", "Lock in round results after all matches for round have been played")]
        [RequireGuildAdmin]
        public async Task LockInRoundAsync(
            [Summary("tournament_id", "The ID of the tournament to round lock")] string tournamentId)
        {
            try
            {
                var result = _lockInRoundLogic.LockInRoundProcess(tournamentId);
                await RespondAsync(embed: result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Command Error: {ex}");
                await RespondAsync("An error occurred while processing this command.", ephemeral: true);
            }
        }

        [SlashCommand("unlock-round", "Unlock the current round to make changes if needed")]
        [RequireGuildAdmin]
        public async Task UnlockRoundAsync(
            [Summary("tournament_id", "The ID of the tournament to unlock the round")] string tournamentId)
        {
            try
            {
                var result = _unlockRoundLogic.UnlockRoundProcess(tournamentId);
                await RespondAsync(embed: result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Command Error: {ex}");
                await RespondAsync("An error occurred while processing this command.", ephemeral: true);
            }
        }

        [SlashCommand("next-round", "Advance to the next round of certain tournaments if conditions are met.")]
        [RequireGuildAdmin]
        public async Task NextRoundAsync(
            [Summary("tournament_id", "The ID of the tournament to advance the round")] string tournamentId)
        {
            try
            {
                var result = _nextRoundLogic.NextRoundProcess(tournamentId);
                await RespondAsync(embed: result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Command Error: {ex}");
                await RespondAsync("An error occurred while processing this command.", ephemeral: true);
            }
        }
    }
}
