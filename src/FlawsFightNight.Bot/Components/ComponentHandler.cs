using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using FlawsFightNight.Commands.StatsCommands.UT2004StatsCommands;
using FlawsFightNight.Services;
using FlawsFightNight.Services.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Bot.Components
{
    public class ComponentHandler : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly EmbedFactory _embedFactory;
        private readonly AdminConfigurationService _adminConfigService;
        private readonly MemberService _memberService;
        private readonly ComparePlayersHandler _comparePlayersHandler;
        private readonly UserLevelLeaderboardHandler _leaderboardHandler;
        private readonly ILogger<ComponentHandler> _logger;

        public ComponentHandler(
            EmbedFactory embedFactory,
            AdminConfigurationService adminConfigService,
            MemberService memberService,
            ComparePlayersHandler comparePlayersHandler,
            UserLevelLeaderboardHandler leaderboardHandler,
            ILogger<ComponentHandler> logger)
        {
            _embedFactory = embedFactory;
            _adminConfigService = adminConfigService;
            _memberService = memberService;
            _comparePlayersHandler = comparePlayersHandler;
            _leaderboardHandler = leaderboardHandler;

            _logger = logger;
        }

        private bool IsAuthorizedUser(ulong expectedUserId) => Context.User.Id == expectedUserId;

        #region Help Select Menu
        [ComponentInteraction("help_select")]
        public async Task HandleHelpSelectAsync(string[] selectedValues)
        {
            try
            {
                var section = selectedValues[0];
                var embed = _embedFactory.HelpSectionEmbed(section);
                var components = ComponentFactory.CreateHelpSelectMenu();

                await (Context.Interaction as SocketMessageComponent)!.UpdateAsync(msg =>
                {
                    msg.Embed = embed;
                    msg.Components = components.Build();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Component error in Help Select.");
                await RespondAsync(embed: _embedFactory.ErrorEmbed($"An error occurred: {ex.Message}"), ephemeral: true);
            }
        }
        #endregion

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
                _logger.LogInformation(AdminFeedEvents.FtpSetupStarted, "FTP setup process initiated by user {UserId} ({Username}).", Context.User.Id, Context.User.Username);
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
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(AdminFeedEvents.FtpSetupFailed, ex, "FTP setup failed for user {UserId}.", Context.User.Id);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(AdminFeedEvents.FtpSetupFailed, ex, "FTP setup failed for user {UserId}.", Context.User.Id);
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
                _logger.LogError(AdminFeedEvents.FtpSetupFailed, ex, "FTP setup cancellation failed for user {UserId}.", Context.User.Id);
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
                        _logger.LogInformation(AdminFeedEvents.FtpSetupCancelled, "FTP setup cancellation confirmed by user {UserId} ({Username}).", Context.User.Id, Context.User.Username);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(AdminFeedEvents.FtpSetupFailed, ex, "FTP setup cancellation error for user {UserId}.", Context.User.Id);
                    }
                    return Task.CompletedTask;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(AdminFeedEvents.FtpSetupFailed, ex, "FTP setup cancellation error for user {UserId}.", Context.User.Id);
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
                _logger.LogError(AdminFeedEvents.FtpSetupFailed, ex, "FTP setup cancellation error for user {UserId}.", Context.User.Id);
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
                _logger.LogError(ex, "Component error in UT2004 Profile Select.");
                await RespondAsync(embed: _embedFactory.ErrorEmbed($"An error occurred: {ex.Message}"), ephemeral: true);
            }
        }
        #endregion

        #region UT2004 Player Profile by GUID Select Menu
        [ComponentInteraction("ut2004profile_guid_select:*")]
        public async Task HandleUT2004ProfileGuidSelectAsync(string guid, string[] selectedValues)
        {
            try
            {
                var utProfile = _memberService.GetUT2004PlayerProfile(guid);
                if (utProfile == null)
                {
                    await RespondAsync(embed: _embedFactory.ErrorEmbed("UT2004 Profile", $"No stats found for GUID `{guid}`."), ephemeral: true);
                    return;
                }

                var selectedSection = selectedValues[0];
                var embed = _embedFactory.UT2004ProfileSectionEmbed(utProfile, selectedSection);
                var components = ComponentFactory.CreateUT2004ProfileSelectMenuByGuid(guid);

                await (Context.Interaction as SocketMessageComponent)!.UpdateAsync(msg =>
                {
                    msg.Embed = embed;
                    msg.Components = components.Build();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Component error in UT2004 Profile GUID Select.");
                await RespondAsync(embed: _embedFactory.ErrorEmbed($"An error occurred: {ex.Message}"), ephemeral: true);
            }
        }
        #endregion

        #region UT2004 Leaderboard Select Menu
        [ComponentInteraction("ut2004leaderboard_select")]
        public async Task HandleUT2004LeaderboardSelectAsync(string[] selectedValues)
        {
            try
            {
                var section = selectedValues[0];
                var embed = _leaderboardHandler.HandleSection(section);
                var components = ComponentFactory.CreateUT2004LeaderboardSelectMenu(section);

                await (Context.Interaction as SocketMessageComponent)!.UpdateAsync(msg =>
                {
                    msg.Embed = embed;
                    msg.Components = components.Build();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Component error in UT2004 Leaderboard Select.");
                await RespondAsync(embed: _embedFactory.ErrorEmbed($"An error occurred: {ex.Message}"), ephemeral: true);
            }
        }

        [ComponentInteraction("ut2004leaderboard_all:*")]
        public async Task HandleUT2004LeaderboardAllAsync(string section)
        {
            try
            {
                await DeferAsync(ephemeral: true);

                var (text, fileName) = _leaderboardHandler.HandleAllSectionAsText(section);

                try
                {
                    var dmChannel = await Context.User.CreateDMChannelAsync();
                    using var ms = new MemoryStream(Encoding.UTF8.GetBytes(text));
                    await dmChannel.SendFileAsync(new FileAttachment(ms, fileName), "📋 Here's the full UT2004 leaderboard you requested!");
                }
                catch
                {
                    await FollowupAsync(embed: _embedFactory.ErrorEmbed("Full Leaderboard", "Could not send you a DM. Please make sure your DMs are open and try again."), ephemeral: true);
                    return;
                }

                await FollowupAsync(embed: _embedFactory.SuccessEmbed("Full Leaderboard", "The full leaderboard has been sent to your DMs! 📬"), ephemeral: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Component error in UT2004 Leaderboard All.");
                await FollowupAsync(embed: _embedFactory.ErrorEmbed($"An error occurred: {ex.Message}"), ephemeral: true);
            }
        }
        #endregion

        #region UT2004 Compare Select Menu
        [ComponentInteraction("ut2004compare_select:*:*")]
        public async Task HandleUT2004CompareSelectAsync(ulong player1Id, ulong player2Id, string[] selectedValues)
        {
            try
            {
                var section = selectedValues[0];
                var embed = _comparePlayersHandler.HandleSection(player1Id, player2Id, section);

                if (embed == null)
                {
                    await RespondAsync(embed: _embedFactory.ErrorEmbed("Compare Players", "Could not load one or both player profiles."), ephemeral: true);
                    return;
                }

                var components = ComponentFactory.CreateUT2004CompareSelectMenu(player1Id, player2Id);

                await (Context.Interaction as SocketMessageComponent)!.UpdateAsync(msg =>
                {
                    msg.Embed = embed;
                    msg.Components = components.Build();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Component error in UT2004 Compare Select.");
                await RespondAsync(embed: _embedFactory.ErrorEmbed($"An error occurred: {ex.Message}"), ephemeral: true);
            }
        }
        #endregion
    }
}
