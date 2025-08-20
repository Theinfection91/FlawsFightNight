using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using FlawsFightNight.CommandsLogic.SetCommands;
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
        [Group("matches_channel_id", "Set or remove the channel ID for matches of a specified tournament")]
        public class MatchesChannelCommands : InteractionModuleBase<SocketInteractionContext>
        {
            private SetMatchesChannelLogic _setMatchesChannelLogic;
            public MatchesChannelCommands(SetMatchesChannelLogic setMatchesChannelLogic)
            {
                _setMatchesChannelLogic = setMatchesChannelLogic;
            }

            [SlashCommand("set", "Set the channel ID for matches of a specified tournament")]
            //[RequireUserPermission(GuildPermission.Administrator)]
            public async Task SetMatchesChannelIdAsync(
            [Summary("tournament_id", "The ID of the tournament to set the matches channel for")] string tournamentId,
            [Summary("channel_id", "The ID of the channel where matches will be posted")] IMessageChannel channel)
            {

                try
                {
                    bool isGuildAdmin = (Context.User as SocketGuildUser)?.GuildPermissions.Administrator ?? false;
                    if (!isGuildAdmin) await RespondAsync($"isGuildAdmin = {isGuildAdmin}", ephemeral: true);
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
            //[RequireUserPermission(GuildPermission.Administrator)]
            public async Task SetMatchesChannelIdAsync(
            [Summary("tournament_id", "The ID of the tournament to set the matches channel for")] string tournamentId)
            {
                try
                {
                    bool isGuildAdmin = (Context.User as SocketGuildUser)?.GuildPermissions.Administrator ?? false;
                    if (!isGuildAdmin) await RespondAsync($"isGuildAdmin = {isGuildAdmin}", ephemeral: true);

                    // TODO: Implement the logic to remove the matches channel ID

                    //var result = ;
                    //await RespondAsync(embed: result);
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
