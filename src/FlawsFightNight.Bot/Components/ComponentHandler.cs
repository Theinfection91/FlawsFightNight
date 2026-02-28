using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using FlawsFightNight.CommandsLogic.SettingsCommands;
using FlawsFightNight.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Bot.Components
{
    public class ComponentHandler : InteractionModuleBase<SocketInteractionContext>
    {
        // Managers and Services
        private readonly EmbedManager _embedManager;
        private readonly ConfigManager _configManager;
        
        // Logic dependencies

        public ComponentHandler(EmbedManager embedManager, ConfigManager configManager)
        {
            _embedManager = embedManager;
            _configManager = configManager;
        }

        private bool IsAuthorizedUser(ulong expectedUserId) => Context.User.Id == expectedUserId;

        #region FTP Setup
        [ComponentInteraction("runftp_confirm:*")]
        public async Task HandleRunFTPConfirmAsync(ulong invokingUserId)
        {
            if (!IsAuthorizedUser(invokingUserId))
            {
                await RespondAsync(embed: _embedManager.ErrorEmbed("This confirmation is not for you."), ephemeral: true);
                return;
            }

            try
            {
                // STEP 1: Update message immediately to show setup is starting
                var statusEmbed = _embedManager.GenericEmbed(
                    "🚀 FTP Setup Initiated", 
                    "Running FTP setup process...\n\n**Go back to the console to continue.**\n\nIf chosen by mistake, you can cancel the process in console or by using `/settings ftp_stats_service cancel_setup`\n\nTo remove existing credentials use `/settings ftp_stats_service remove_credentials`", 
                    Color.Blue);

                await (Context.Interaction as SocketMessageComponent)!.UpdateAsync(msg =>
                {
                    msg.Content = null;
                    msg.Embed = statusEmbed;
                    msg.Components = new ComponentBuilder().Build(); // remove buttons
                });

                // STEP 2: Fire off the FTP setup in background (don't await, don't block)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _configManager.FTPSetupProcess(isUserInit: true);
                        Console.WriteLine($"{DateTime.Now} - [ComponentHandler] FTP setup completed via Discord command.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{DateTime.Now} - [ComponentHandler] FTP setup error: {ex}");
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Component Error - FTP Confirm] {ex}");
                await RespondAsync(embed: _embedManager.ErrorEmbed($"An error occurred while running FTP setup: {ex.Message}"), ephemeral: true);
            }
        }

        [ComponentInteraction("runftp_cancel:*")]
        public async Task HandleRunFTPCancelAsync(ulong invokingUserId)
        {
            if (!IsAuthorizedUser(invokingUserId))
            {
                await RespondAsync(embed: _embedManager.ErrorEmbed("This cancellation is not for you."), ephemeral: true);
                return;
            }

            try
            {
                await (Context.Interaction as SocketMessageComponent)!.UpdateAsync(msg =>
                {
                    msg.Content = "❌ FTP setup cancelled.";
                    msg.Embed = null;
                    msg.Components = new ComponentBuilder().Build();
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Component Error - FTP Cancel] {ex}");
                await RespondAsync(embed: _embedManager.ErrorEmbed($"An error occurred: {ex.Message}"), ephemeral: true);
            }
        }
        #endregion
    }
}
