using Discord;
using Discord.Interactions;
using FlawsFightNight.Bot.Components;
using FlawsFightNight.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace FlawsFightNight.Bot.SlashCommands
{
    public class HelpCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly EmbedFactory _embedFactory;
        private readonly ILogger<HelpCommands> _logger;

        public HelpCommands(EmbedFactory embedFactory, ILogger<HelpCommands> logger)
        {
            _embedFactory = embedFactory;
            _logger = logger;
        }

        [SlashCommand("help", "Displays an interactive help guide for all bot commands and features.")]
        public async Task HelpAsync()
        {
            try
            {
                await DeferAsync(ephemeral: true);
                var embed = _embedFactory.HelpSectionEmbed("overview");
                var components = ComponentFactory.CreateHelpSelectMenu();
                await FollowupAsync(embed: embed, components: components.Build(), ephemeral: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Command error in {Command}.", nameof(HelpAsync));
                await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
            }
        }
    }
}