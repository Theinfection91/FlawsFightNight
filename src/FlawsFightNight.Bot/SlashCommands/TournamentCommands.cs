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

        public TournamentCommands(CreateTournamentLogic createTournamentLogic)
        {
            _createTournamentLogic = createTournamentLogic;
        }

        [SlashCommand("create", "Create a new tournament")]
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
    }
}
