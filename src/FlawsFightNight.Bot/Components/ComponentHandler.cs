using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using FlawsFightNight.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FlawsFightNight.Bot.Components
{
    public class ComponentHandler : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly EmbedFactory _embedFactory;
        private readonly AdminConfigurationService _adminConfigService;
        private readonly MemberService _memberService;

        public ComponentHandler(EmbedFactory embedFactory, AdminConfigurationService adminConfigService, MemberService memberService)
        {
            _embedFactory = embedFactory;
            _adminConfigService = adminConfigService;
            _memberService = memberService;
        }

        private bool IsAuthorizedUser(ulong expectedUserId) => Context.User.Id == expectedUserId;

        #region FTP Setup Run
        [ComponentInteraction("runftp_confirm:*")]
        public async Task HandleRunFTPConfirmAsync(ulong invokingUserId)
        {
            if (!IsAuthorizedUser(invokingUserId))
            {
                await RespondAsync(embed: _embedFactory.ErrorEmbed("This confirmation is not for you."), ephemeral: true);
                return;
            }

            try
            {
                var statusEmbed = _embedFactory.GenericEmbed(
                    "🚀 FTP Setup Initiated",
                    "Running FTP setup process...\n\n**Go back to the console to continue.**\n\nIf chosen by mistake, you can cancel the process in console or by using `/settings ftp_stats_service cancel_setup`\n\nTo remove existing credentials use `/settings ftp_stats_service remove_credentials`",
                    Color.Blue);

                await (Context.Interaction as SocketMessageComponent)!.UpdateAsync(msg =>
                {
                    msg.Content = null;
                    msg.Embed = statusEmbed;
                    msg.Components = new ComponentBuilder().Build();
                });

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _adminConfigService.FTPSetupProcess(isUserInit: true);
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
                await RespondAsync(embed: _embedFactory.ErrorEmbed($"An error occurred while running FTP setup: {ex.Message}"), ephemeral: true);
            }
        }

        [ComponentInteraction("runftp_cancel:*")]
        public async Task HandleRunFTPCancelAsync(ulong invokingUserId)
        {
            if (!IsAuthorizedUser(invokingUserId))
            {
                await RespondAsync(embed: _embedFactory.ErrorEmbed("This cancellation is not for you."), ephemeral: true);
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
                await RespondAsync(embed: _embedFactory.ErrorEmbed($"An error occurred: {ex.Message}"), ephemeral: true);
            }
        }
        #endregion

        #region FTP Setup Cancel
        [ComponentInteraction("cancelftp_confirm:*")]
        public async Task HandleCancelFTPConfirmAsync(ulong invokingUserId)
        {
            if (!IsAuthorizedUser(invokingUserId))
            {
                await RespondAsync(embed: _embedFactory.ErrorEmbed("This cancellation is not for you."), ephemeral: true);
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

                _ = Task.Run(() =>
                {
                    try
                    {
                        _adminConfigService.NotifyCancelFTPSetupProcess();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{DateTime.Now} - [ComponentHandler] FTP setup cancellation error: {ex}");
                    }
                    return Task.CompletedTask;
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Component Error - FTP Cancel] {ex}");
                await RespondAsync(embed: _embedFactory.ErrorEmbed($"An error occurred: {ex.Message}"), ephemeral: true);
            }
        }

        [ComponentInteraction("cancelftp_cancel:*")]
        public async Task HandleCancelFTPCancelAsync(ulong invokingUserId)
        {
            if (!IsAuthorizedUser(invokingUserId))
            {
                await RespondAsync(embed: _embedFactory.ErrorEmbed("This cancellation is not for you."), ephemeral: true);
                return;
            }

            try
            {
                await (Context.Interaction as SocketMessageComponent)!.UpdateAsync(msg =>
                {
                    msg.Content = "❌ FTP setup cancellation aborted.";
                    msg.Embed = null;
                    msg.Components = new ComponentBuilder().Build();
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Component Error - FTP Cancel] {ex}");
                await RespondAsync(embed: _embedFactory.ErrorEmbed($"An error occurred: {ex.Message}"), ephemeral: true);
            }
        }
        #endregion

        #region UT2004 Player Profile Select Menu
        [ComponentInteraction("ut2004profile_select:*")]
        public async Task HandleUT2004ProfileSelectAsync(ulong invokingUserId, string[] selectedValues)
        {
            if (!IsAuthorizedUser(invokingUserId))
            {
                await RespondAsync(embed: _embedFactory.ErrorEmbed("This menu is not for you."), ephemeral: true);
                return;
            }

            try
            {
                var memberProfile = _memberService.GetMemberProfile(invokingUserId);
                if (memberProfile == null || memberProfile.RegisteredUT2004GUIDs.Count == 0)
                {
                    await RespondAsync(embed: _embedFactory.ErrorEmbed("UT2004 Profile", "No UT2004 GUID registered to your account."), ephemeral: true);
                    return;
                }

                var guid = memberProfile.RegisteredUT2004GUIDs.First();
                var utProfile = _memberService.GetUT2004PlayerProfile(guid);
                if (utProfile == null)
                {
                    await RespondAsync(embed: _embedFactory.ErrorEmbed("UT2004 Profile", $"No stats found for GUID `{guid}`."), ephemeral: true);
                    return;
                }

                var selectedSection = selectedValues[0];
                var embed = _embedFactory.UT2004ProfileSectionEmbed(utProfile, selectedSection);
                var components = ComponentFactory.CreateUT2004ProfileSelectMenu(invokingUserId);

                await (Context.Interaction as SocketMessageComponent)!.UpdateAsync(msg =>
                {
                    msg.Embed = embed;
                    msg.Components = components.Build();
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Component Error - UT2004 Profile Select] {ex}");
                await RespondAsync(embed: _embedFactory.ErrorEmbed($"An error occurred: {ex.Message}"), ephemeral: true);
            }
        }
        #endregion
    }
}
