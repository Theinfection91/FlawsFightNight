using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using FlawsFightNight.Bot.PreconditionAttributes;
using FlawsFightNight.CommandsLogic.SetCommands;
using FlawsFightNight.CommandsLogic.SettingsCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Bot.SlashCommands
{
    [Group("settings", "Commands for tournament and admin settings")]
    public class SettingsCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private AddDebugAdminLogic _addDebugAdminLogic;
        public SettingsCommands(AddDebugAdminLogic addDebugAdminLogic)
        {
            _addDebugAdminLogic = addDebugAdminLogic;
        }

        #region Debug Commands
        [SlashCommand("add_debug_admin", "Add a user as a debug admin")]
        [RequireGuildAdmin]
        public async Task AddDebugAdminAsync(
            [Summary("user", "The user to add as a debug admin")] IUser user)
        {
            try
            {
                var result = _addDebugAdminLogic.AddDebugAdminProcess(user.Id);
                await RespondAsync(embed: result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Command Error: {ex}");
                await RespondAsync("An error occurred while processing this command.", ephemeral: true);
            }
        }
        #endregion

        [Group("matches_channel_id", "Set or remove the channel ID for matches of a specified tournament")]
        public class MatchesChannelCommands : InteractionModuleBase<SocketInteractionContext>
        {
            private SetMatchesChannelLogic _setMatchesChannelLogic;
            public MatchesChannelCommands(SetMatchesChannelLogic setMatchesChannelLogic)
            {
                _setMatchesChannelLogic = setMatchesChannelLogic;
            }

            [SlashCommand("set", "Set the channel ID for matches of a specified tournament")]
            [RequireGuildAdmin]
            public async Task SetMatchesChannelIdAsync(
            [Summary("tournament_id", "The ID of the tournament to set the matches channel for")] string tournamentId,
            [Summary("channel_id", "The ID of the channel where matches will be posted")] IMessageChannel channel)
            {

                try
                {
                    var result = _setMatchesChannelLogic.SetMatchesChannelProcess(tournamentId, channel);
                    await RespondAsync(embed: result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Command Error: {ex}");
                    await RespondAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }

            [SlashCommand("remove", "Remove the channel ID for matches of a specified tournament")]
            [RequireGuildAdmin]
            public async Task SetMatchesChannelIdAsync(
            [Summary("tournament_id", "The ID of the tournament to set the matches channel for")] string tournamentId)
            {
                try
                {
                    // TODO: Implement the logic to remove the matches channel ID

                    //var result = ;
                    //await RespondAsync(embed: result);

                    await RespondAsync("TODO");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Command Error: {ex}");
                    await RespondAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }
        }
    }
}
