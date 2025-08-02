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
        private LockTeamsLogic _lockTeamsLogic;
        private StartTournamentLogic _startTournamentLogic;

        public TournamentCommands(CreateTournamentLogic createTournamentLogic, LockTeamsLogic lockTeamsLogic, StartTournamentLogic startTournamentLogic)
        {
            _createTournamentLogic = createTournamentLogic;
            _lockTeamsLogic = lockTeamsLogic;
            _startTournamentLogic = startTournamentLogic;
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

        [SlashCommand("start", "Start a tournament")]
        [Discord.Commands.RequireUserPermission(GuildPermission.Administrator)]
        public async Task StartTournamentAsync(
            [Summary("tournament_id", "The ID of the tournament to start")] string tournamentId)
        {
            try
            {
                var result = _startTournamentLogic.StartTournamentProcess(tournamentId);
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
