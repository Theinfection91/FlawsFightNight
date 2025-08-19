using Discord;
using Discord.Interactions;
using FlawsFightNight.CommandsLogic.SetCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Bot.SlashCommands
{
    [Group("set", "Commands to set various configurations for tournaments")]
    public class SetCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private SetMatchesChannelLogic _setMatchesChannelLogic;
        public SetCommands(SetMatchesChannelLogic setMatchesChannelLogic)
        {
            _setMatchesChannelLogic = setMatchesChannelLogic;
        }

        [SlashCommand("matches_channel_id", "Set the channel ID for matches of a specified tournament")]
        [Discord.Commands.RequireUserPermission(GuildPermission.Administrator)]
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
    }
}
