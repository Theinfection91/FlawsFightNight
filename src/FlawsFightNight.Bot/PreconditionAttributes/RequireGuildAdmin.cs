using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Bot.PreconditionAttributes
{
    public class RequireGuildAdmin : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            var user = context.User as SocketGuildUser;
            if (user != null && user.GuildPermissions.Administrator)
            {
                return PreconditionResult.FromSuccess();
            }
            else
            {
                await context.Interaction.RespondAsync("❌ You must be a guild administrator to use this command.", ephemeral: true);
                return PreconditionResult.FromError("You must be a guild administrator to use this command.");
            }
        }
    }
}
