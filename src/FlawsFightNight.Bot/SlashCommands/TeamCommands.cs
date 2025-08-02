using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Bot.SlashCommands
{
    [Group("team", "Commands related to teams like creating, removal, etc.")]
    public class TeamCommands : InteractionModuleBase<SocketInteractionContext>
    {
        public TeamCommands() { }

        [SlashCommand("register", "Register a new team for a chosen Tournament")]
        [Discord.Commands.RequireUserPermission(GuildPermission.Administrator)]
        public async Task RegisterTeamAsync(
            [Summary("name", "The name of the team")] string name,
            [Summary("tournament_id", "The ID of the tournament to register for")] string tournamentId,
            [Summary("member1", "A member to add to the team.")] IUser member1,
            [Summary("member2", "A member to add to the team.")] IUser? member2 = null,
            [Summary("member3", "A member to add to the team.")] IUser? member3 = null,
            [Summary("member4", "A member to add to the team.")] IUser? member4 = null,
            [Summary("member5", "A member to add to the team.")] IUser? member5 = null,
            [Summary("member6", "A member to add to the team.")] IUser? member6 = null,
            [Summary("member7", "A member to add to the team.")] IUser? member7 = null,
            [Summary("member8", "A member to add to the team.")] IUser? member8 = null,
            [Summary("member9", "A member to add to the team.")] IUser? member9 = null,
            [Summary("member10", "A member to add to the team.")] IUser? member10 = null,
            [Summary("member11", "A member to add to the team.")] IUser? member11 = null,
            [Summary("member12", "A member to add to the team.")] IUser? member12 = null,
            [Summary("member13", "A member to add to the team.")] IUser? member13 = null,
            [Summary("member14", "A member to add to the team.")] IUser? member14 = null,
            [Summary("member15", "A member to add to the team.")] IUser? member15 = null,
            [Summary("member16", "A member to add to the team.")] IUser? member16 = null,
            [Summary("member17", "A member to add to the team.")] IUser? member17 = null,
            [Summary("member18", "A member to add to the team.")] IUser? member18 = null,
            [Summary("member19", "A member to add to the team.")] IUser? member19 = null,
            [Summary("member20", "A member to add to the team.")] IUser? member20 = null)
        {
            try
            {
                // Logic to register the team goes here
                // For now, we will just respond with a success message
                await RespondAsync($"Team '{name}' has been registered for tournament ID {tournamentId}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Command Error: {ex}");
                await RespondAsync("An error occurred while processing this command.", ephemeral: true);
            }
        }
    }
}
