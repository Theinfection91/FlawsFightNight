using Discord;
using Discord.Interactions;
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
        private StartTournamentLogic _startTournamentLogic;
        private UnlockRoundLogic _unlockRoundLogic;
        private UnlockTeamsLogic _unlockTeamsLogic;

        public TournamentCommands(CreateTournamentLogic createTournamentLogic, EndTournamentLogic endTournamentLogic, LockInRoundLogic lockInRoundLogic, LockTeamsLogic lockTeamsLogic, NextRoundLogic nextRoundLogic, StartTournamentLogic startTournamentLogic, UnlockRoundLogic unlockRoundLogic, UnlockTeamsLogic unlockTeamsLogic)
        {
            _createTournamentLogic = createTournamentLogic;
            _endTournamentLogic = endTournamentLogic;
            _lockInRoundLogic = lockInRoundLogic;
            _lockTeamsLogic = lockTeamsLogic;
            _nextRoundLogic = nextRoundLogic;
            _startTournamentLogic = startTournamentLogic;
            _unlockRoundLogic = unlockRoundLogic;
            _unlockTeamsLogic = unlockTeamsLogic;
        }

        [SlashCommand("create", "Create a new tournament")]
        [Discord.Commands.RequireUserPermission(GuildPermission.Administrator)]
        public async Task CreateTournamentAsync(
            [Summary("name", "The name of the tournament")] string name,
            [Summary("type", "The type of the tournament")] TournamentType tournamentType,
            [Summary("team_size", "The size of each team in the tournament")] int teamSize,
            [Summary("description", "A description of the tournament (optional)")] string? description = null)
        {
            try
            {
                var result = _createTournamentLogic.CreateTournamentProcess(Context, name, tournamentType, teamSize, description);
                await RespondAsync(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Command Error: {ex}");
                await RespondAsync("An error occurred while processing this command.", ephemeral: true);
            }
        }

        [SlashCommand("lock-teams", "Lock teams in a tournament")]
        [Discord.Commands.RequireUserPermission(GuildPermission.Administrator)]
        public async Task LockTeamsAsync(
            [Summary("tournament_id", "The ID of the tournament to lock teams in")] string tournamentId)
        {
            try
            {
                var result = _lockTeamsLogic.LockTeamsProcess(Context, tournamentId);
                await RespondAsync(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Command Error: {ex}");
                await RespondAsync("An error occurred while processing this command.", ephemeral: true);
            }
        }

        [SlashCommand("unlock-teams", "Unlock teams in a tournament")]
        [Discord.Commands.RequireUserPermission(GuildPermission.Administrator)]
        public async Task UnlockTeamsAsync(
            [Summary("tournament_id", "The ID of the tournament to unlock teams in")] string tournamentId)
        {
            try
            {
                var result = _unlockTeamsLogic.UnlockTeamsProcess(tournamentId);
                await RespondAsync(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Command Error: {ex}");
                await RespondAsync("An error occurred while processing this command.", ephemeral: true);
            }
        }

        [SlashCommand("start", "Start a tournament")]
        [Discord.Commands.RequireUserPermission(GuildPermission.Administrator)]
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

        [SlashCommand("end", "End a tournament")]
        [Discord.Commands.RequireUserPermission(GuildPermission.Administrator)]
        public async Task EndTournamentAsync(
            [Summary("tournament_id", "The ID of the tournament to end")] string tournamentId)
        {
            try
            {
                var result = _endTournamentLogic.EndTournamentProcess(tournamentId);
                await RespondAsync(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Command Error: {ex}");
                await RespondAsync("An error occurred while processing this command.", ephemeral: true);
            }
        }

        [SlashCommand("lock-in-round", "Lock in round results after all matches for round have been played")]
        [Discord.Commands.RequireUserPermission(GuildPermission.Administrator)]
        public async Task LockInRoundAsync(
            [Summary("tournament_id", "The ID of the tournament to round lock")] string tournamentId)
        {
            try
            {
                var result = _lockInRoundLogic.LockInRoundProcess(tournamentId);
                await RespondAsync(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Command Error: {ex}");
                await RespondAsync("An error occurred while processing this command.", ephemeral: true);
            }
        }

        [SlashCommand("unlock-round", "Unlock the current round to make changes if needed")]
        [Discord.Commands.RequireUserPermission(GuildPermission.Administrator)]
        public async Task UnlockRoundAsync(
            [Summary("tournament_id", "The ID of the tournament to unlock the round")] string tournamentId)
        {
            try
            {
                var result = _unlockRoundLogic.UnlockRoundProcess(tournamentId);
                await RespondAsync(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Command Error: {ex}");
                await RespondAsync("An error occurred while processing this command.", ephemeral: true);
            }
        }

        [SlashCommand("next-round", "Advance to the next round of certain tournaments if conditions are met.")]
        [Discord.Commands.RequireUserPermission(GuildPermission.Administrator)]
        public async Task NextRoundAsync(
            [Summary("tournament_id", "The ID of the tournament to advance the round")] string tournamentId)
        {
            try
            {
                var result = _nextRoundLogic.NextRoundProcess(tournamentId);
                await RespondAsync(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Command Error: {ex}");
                await RespondAsync("An error occurred while processing this command.", ephemeral: true);
            }
        }
    }
}
