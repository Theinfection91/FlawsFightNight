using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using FlawsFightNight.Bot.Autocomplete;
using FlawsFightNight.Bot.Components;
using FlawsFightNight.Bot.Attributes;
using FlawsFightNight.Commands.SettingsCommands;
using FlawsFightNight.Commands.SettingsCommands.UT2004AdminCommands;
using FlawsFightNight.Commands.SettingsCommands.AdminChannelFeedCommands;
using FlawsFightNight.Core.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Bot.SlashCommands
{
    [Group("settings", "Commands for tournament and admin settings")]
    [RequireGuildAdmin]
    public class SettingsCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly AddDebugAdminHandler _addDebugAdminLogic;
        private readonly RemoveDebugAdminHandler _removeDebugAdminLogic;
        private readonly ILogger<SettingsCommands> _logger;

        public SettingsCommands(AddDebugAdminHandler addDebugAdminLogic, RemoveDebugAdminHandler removeDebugAdminLogic, ILogger<SettingsCommands> logger)
        {
            _addDebugAdminLogic = addDebugAdminLogic;
            _removeDebugAdminLogic = removeDebugAdminLogic;
            _logger = logger;
        }

        #region Debug Commands
        [SlashCommand("add_debug_admin", "Add a user as a debug admin")]
        public async Task AddDebugAdminAsync(
            [Summary("user", "The user to add as a debug admin")] IUser user)
        {
            try
            {
                await DeferAsync();
                var result = await _addDebugAdminLogic.AddDebugAdminProcess(user.Id);
                await FollowupAsync(embed: result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Command error in {Command}.", nameof(AddDebugAdminAsync));
                await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
            }
        }

        [SlashCommand("remove_debug_admin", "Remove a user from debug admins")]
        public async Task RemoveDebugAdminAsync(
            [Summary("user", "The user to remove from debug admins")] IUser user)
        {
            try
            {
                await DeferAsync();
                var result = await _removeDebugAdminLogic.RemoveDebugAdminProcess(user.Id);
                await FollowupAsync(embed: result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Command error in {Command}.", nameof(RemoveDebugAdminAsync));
                await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
            }
        }
        #endregion

        #region The Feed Channel Commands
        [Group("admin_feed_channel", "Set or remove a channel as the UT2004 LiveView admin feed channel for updates and logging")]
        public class AdminFeedChannelCommands : InteractionModuleBase<SocketInteractionContext>
        {
            private readonly SetAdminChannelFeedHandler _setAdminFeedChannelHandler;
            private readonly RemoveAdminChannelFeedHandler _removeAdminFeedChannelHandler;
            private readonly ILogger<AdminFeedChannelCommands> _logger;

            public AdminFeedChannelCommands(SetAdminChannelFeedHandler setAdminFeedChannelHandler, RemoveAdminChannelFeedHandler removeAdminFeedChannelHandler, ILogger<AdminFeedChannelCommands> logger)
            {
                _setAdminFeedChannelHandler = setAdminFeedChannelHandler;
                _removeAdminFeedChannelHandler = removeAdminFeedChannelHandler;
                _logger = logger;
            }

            [SlashCommand("set", "Register a channel as the UT2004 LiveView admin feed channel for updates and logging.")]
            public async Task SetAdminFeedChannelAsync(
                [Summary("channel", "The channel to post admin updates and logs in")] IMessageChannel channel)
            {
                try
                {
                    await DeferAsync();
                    var result = await _setAdminFeedChannelHandler.Handle(channel);
                    await FollowupAsync(embed: result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Command error in {Command}.", nameof(SetAdminFeedChannelAsync));
                    await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }

            [SlashCommand("remove", "Unregister current channel from the UT2004 LiveView admin feed if set.")]
            public async Task RemoveAdminFeedChannelAsync()
            {
                try
                {
                    await DeferAsync();
                    var result = await _removeAdminFeedChannelHandler.Handle();
                    await FollowupAsync(embed: result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Command error in {Command}.", nameof(RemoveAdminFeedChannelAsync));
                    await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }
        }
        #endregion

        #region Leaderboard Channel Commands
        [Group("leaderboard_channel", "Set or remove a channel as a UT2004 LiveView leaderboard channel")]
        public class LeaderboardChannelCommands : InteractionModuleBase<SocketInteractionContext>
        {
            private readonly SetLeaderboardChannelHandler _setLeaderboardChannelHandler;
            private readonly RemoveLeaderboardChannelHandler _removeLeaderboardChannelHandler;
            private readonly ILogger<LeaderboardChannelCommands> _logger;

            public LeaderboardChannelCommands(
                SetLeaderboardChannelHandler setLeaderboardChannelHandler,
                RemoveLeaderboardChannelHandler removeLeaderboardChannelHandler,
                ILogger<LeaderboardChannelCommands> logger)
            {
                _setLeaderboardChannelHandler = setLeaderboardChannelHandler;
                _removeLeaderboardChannelHandler = removeLeaderboardChannelHandler;
                _logger = logger;
            }

            [SlashCommand("set", "Register a channel as a UT2004 leaderboard LiveView channel.")]
            public async Task SetLeaderboardChannelAsync(
                [Summary("channel", "The channel to post the leaderboard in")] IMessageChannel channel,
                [Summary("default_view", "Which category this channel defaults to on each refresh (the dropdown always lets users switch)")]
                [Choice("📊 General", 3)]
                [Choice("🚩 iCTF", 1)]
                [Choice("🎯 TAM", 2)]
                [Choice("💣 iBR", 0)] int defaultType = 3)
            {
                try
                {
                    await DeferAsync();
                    var type = (LeaderboardChannelTypes)defaultType;
                    var result = await _setLeaderboardChannelHandler.Handle(channel, type);
                    await FollowupAsync(embed: result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Command error in {Command}.", nameof(SetLeaderboardChannelAsync));
                    await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }

            [SlashCommand("remove", "Unregister a channel from the UT2004 leaderboard LiveView.")]
            public async Task RemoveLeaderboardChannelAsync(
                [Summary("channel", "The channel to remove from leaderboard LiveView")] IMessageChannel channel)
            {
                try
                {
                    await DeferAsync();
                    var result = await _removeLeaderboardChannelHandler.Handle(channel);
                    await FollowupAsync(embed: result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Command error in {Command}.", nameof(RemoveLeaderboardChannelAsync));
                    await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }
        }
        #endregion

        [Group("matches_channel_id", "Set or remove the channel ID for matches of a specified tournament")]
        public class MatchesChannelCommands : InteractionModuleBase<SocketInteractionContext>
        {
            private readonly SetMatchesChannelHandler _setMatchesChannelLogic;
            private readonly RemoveMatchesChannelHandler _removeMatchesChannelLogic;
            private readonly ILogger<MatchesChannelCommands> _logger;

            public MatchesChannelCommands(SetMatchesChannelHandler setMatchesChannelLogic, RemoveMatchesChannelHandler removeMatchesChannelLogic, ILogger<MatchesChannelCommands> logger)
            {
                _setMatchesChannelLogic = setMatchesChannelLogic;
                _removeMatchesChannelLogic = removeMatchesChannelLogic;
                _logger = logger;
            }

            [SlashCommand("set", "Set the channel ID for matches of a specified tournament")]
            public async Task SetMatchesChannelIdAsync(
                [Summary("tournament_id", "The ID of the tournament to set the matches channel for"), Autocomplete(typeof(TournamentIdAutocomplete))] string tournamentId,
                [Summary("channel_id", "The ID of the channel where matches will be posted")] IMessageChannel channel)
            {
                try
                {
                    await DeferAsync();
                    var result = await _setMatchesChannelLogic.SetMatchesChannelProcess(tournamentId, channel);
                    await FollowupAsync(embed: result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Command error in {Command}.", nameof(SetMatchesChannelIdAsync));
                    await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }

            [SlashCommand("remove", "Remove the channel ID for matches of a specified tournament")]
            public async Task RemoveMatchesChannelIdAsync(
                [Summary("tournament_id", "The ID of the tournament to stop the matches LiveView."), Autocomplete(typeof(TournamentIdAutocomplete))] string tournamentId)
            {
                try
                {
                    await DeferAsync();
                    var result = await _removeMatchesChannelLogic.RemoveMatchesChannelProcess(tournamentId);
                    await FollowupAsync(embed: result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Command error in {Command}.", nameof(RemoveMatchesChannelIdAsync));
                    await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }
        }

        [Group("standings_channel_id", "Set or remove the channel ID for standings of a specified tournament")]
        public class StandingsChannelCommands : InteractionModuleBase<SocketInteractionContext>
        {
            private readonly SetStandingsChannelHandler _setStandingsChannelLogic;
            private readonly RemoveStandingsChannelHandler _removeStandingsChannelLogic;
            private readonly ILogger<StandingsChannelCommands> _logger;

            public StandingsChannelCommands(SetStandingsChannelHandler setStandingsChannelLogic, RemoveStandingsChannelHandler removeStandingsChannelLogic, ILogger<StandingsChannelCommands> logger)
            {
                _setStandingsChannelLogic = setStandingsChannelLogic;
                _removeStandingsChannelLogic = removeStandingsChannelLogic;
                _logger = logger;
            }

            [SlashCommand("set", "Set the channel ID for standings of a specified tournament")]
            public async Task SetStandingsChannelIdAsync(
                [Summary("tournament_id", "The ID of the tournament to set the standings channel for"), Autocomplete(typeof(TournamentIdAutocomplete))] string tournamentId,
                [Summary("channel_id", "The ID of the channel where standings will be posted")] IMessageChannel channel)
            {
                try
                {
                    await DeferAsync();
                    var result = await _setStandingsChannelLogic.SetStandingsChannelProcess(tournamentId, channel);
                    await FollowupAsync(embed: result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Command error in {Command}.", nameof(SetStandingsChannelIdAsync));
                    await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }

            [SlashCommand("remove", "Remove the channel ID for standings of a specified tournament")]
            public async Task RemoveStandingsChannelIdAsync(
                [Summary("tournament_id", "The ID of the tournament to stop the standings LiveView."), Autocomplete(typeof(TournamentIdAutocomplete))] string tournamentId)
            {
                try
                {
                    await DeferAsync();
                    var result = await _removeStandingsChannelLogic.RemoveStandingsChannelProcess(tournamentId);
                    await FollowupAsync(embed: result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Command error in {Command}.", nameof(RemoveStandingsChannelIdAsync));
                    await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }
        }

        [Group("teams_channel_id", "Set or remove the channel ID for teams of a specified tournament")]
        public class TeamsChannelCommands : InteractionModuleBase<SocketInteractionContext>
        {
            private readonly SetTeamsChannelHandler _setTeamsChannelLogic;
            private readonly RemoveTeamsChannelHandler _removeTeamsChannelLogic;
            private readonly ILogger<TeamsChannelCommands> _logger;

            public TeamsChannelCommands(SetTeamsChannelHandler setTeamsChannelLogic, RemoveTeamsChannelHandler removeTeamsChannelLogic, ILogger<TeamsChannelCommands> logger)
            {
                _setTeamsChannelLogic = setTeamsChannelLogic;
                _removeTeamsChannelLogic = removeTeamsChannelLogic;
                _logger = logger;
            }

            [SlashCommand("set", "Set the channel ID for teams of a specified tournament")]
            public async Task SetTeamsChannelIdAsync(
                [Summary("tournament_id", "The ID of the tournament to set the teams channel for"), Autocomplete(typeof(TournamentIdAutocomplete))] string tournamentId,
                [Summary("channel_id", "The ID of the channel where teams will be posted")] IMessageChannel channel)
            {
                try
                {
                    await DeferAsync();
                    var result = await _setTeamsChannelLogic.SetTeamsChannelProcess(tournamentId, channel);
                    await FollowupAsync(embed: result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Command error in {Command}.", nameof(SetTeamsChannelIdAsync));
                    await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }

            [SlashCommand("remove", "Remove the channel ID for teams of a specified tournament")]
            public async Task RemoveTeamsChannelIdAsync(
                [Summary("tournament_id", "The ID of the tournament to stop the teams LiveView."), Autocomplete(typeof(TournamentIdAutocomplete))] string tournamentId)
            {
                try
                {
                    await DeferAsync();
                    var result = await _removeTeamsChannelLogic.RemoveTeamsChannelProcess(tournamentId);
                    await FollowupAsync(embed: result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Command error in {Command}.", nameof(RemoveTeamsChannelIdAsync));
                    await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }
        }

        [Group("ftp_stats_service", "Re-run FTP Setup Process in Console and remove FTP credentials")]
        public class FTPStatsServiceCommands : InteractionModuleBase<SocketInteractionContext>
        {
            private readonly AutocompleteCache _autocompleteCache;
            private readonly RemoveFTPCredentialsHandler _removeFTPCredentialsLogic;
            private readonly ILogger<FTPStatsServiceCommands> _logger;

            public FTPStatsServiceCommands(AutocompleteCache autocompleteCache, RemoveFTPCredentialsHandler removeFTPCredentialsLogic, ILogger<FTPStatsServiceCommands> logger)
            {
                _autocompleteCache = autocompleteCache;
                _removeFTPCredentialsLogic = removeFTPCredentialsLogic;
                _logger = logger;
            }

            [SlashCommand("run_setup", "Re-run the FTP Setup Process in Console to add FTP credentials or change FTP server")]
            public async Task RunFTPSetupAsync()
            {
                try
                {
                    await DeferAsync(ephemeral: true);
                    var components = ComponentFactory.CreateConfirmationCancelButtons("runftp", Context.User.Id);
                    await FollowupAsync("⚠️ **This will re-run FTP setup in the console.\n\nRepeat: Setup is done in the console, not Discord. If console cannot be reached and this was done by mistake then this can be terminated by using `/settings ftp_stats_service cancel_setup`**\n\nAre you sure you want to continue?", components: components.Build(), ephemeral: true);
                    _autocompleteCache.Update();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Command error in {Command}.", nameof(RunFTPSetupAsync));
                    await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }

            [SlashCommand("remove_credentials", "Remove specific FTP credentials from the database")]
            public async Task RemoveFTPCredentialsAsync(
                [Summary("ftp_credential_id", "The FTP credential by ID to remove"), Autocomplete(typeof(FTPCredentialAutocomplete))] string ftpServerName)
            {
                try
                {
                    await DeferAsync(ephemeral: true);
                    var result = await _removeFTPCredentialsLogic.RemoveFTPCredentialsProcess(ftpServerName);
                    await FollowupAsync(embed: result, ephemeral: true);
                    _autocompleteCache.Update();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Command error in {Command}.", nameof(RemoveFTPCredentialsAsync));
                    await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }

            [SlashCommand("cancel_setup", "Cancel the FTP setup process that is currently running in the console")]
            public async Task CancelFTPSetupAsync()
            {
                try
                {
                    await DeferAsync(ephemeral: true);
                    var components = ComponentFactory.CreateConfirmationCancelButtons("cancelftp", Context.User.Id);
                    await FollowupAsync("⚠️ Confirm FTP Setup Cancellation\n\nAre you sure you want to cancel the FTP setup process? This will stop the ongoing setup and any progress will be lost. You can always run the setup again later if needed.", components: components.Build(), ephemeral: true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Command error in {Command}.", nameof(CancelFTPSetupAsync));
                    await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }
        }

        [Group("ut2004", "Admin commands related to UT2004 data")]
        public class UT2004Commands : InteractionModuleBase<SocketInteractionContext>
        {
            private readonly AutocompleteCache _autocompleteCache;
            private readonly GetLogsByIDHandler _getLogsByIDHandler;
            private readonly IgnoreLogsByIDHandler _ignoreLogsByIDHandler;
            private readonly AllowLogsByIDHandler _allowLogsByIDHandler;
            private readonly StatLogsByDateHandler _statLogsByDateHandler;
            private readonly TagLogToMatchHandler _tagLogToMatchHandler;
            private readonly RegisterGuidToMemberHandler _registerGuidToMemberLogic;
            private readonly RemoveGuidFromMemberHandler _removeGuidFromMemberLogic;
            private readonly LastStatLogsHandler _lastStatLogsHandler;
            private readonly UnTagLogToMatchHandler _unTagLogToMatchHandler;
            private readonly ILogger<UT2004Commands> _logger;

            public UT2004Commands(
                AutocompleteCache autocompleteCache,
                GetLogsByIDHandler getLogsByIDHandler,
                IgnoreLogsByIDHandler ignoreLogsByIDHandler,
                AllowLogsByIDHandler allowLogsByIDHandler,
                StatLogsByDateHandler statLogsByDateHandler,
                RegisterGuidToMemberHandler registerGuidToMemberLogic,
                RemoveGuidFromMemberHandler removeGuidFromMemberLogic,
                LastStatLogsHandler lastStatLogsHandler,
                TagLogToMatchHandler tagLogToMatchHandler,
                UnTagLogToMatchHandler unTagLogToMatchHandler,
                ILogger<UT2004Commands> logger)
            {
                _autocompleteCache = autocompleteCache;
                _getLogsByIDHandler = getLogsByIDHandler;
                _ignoreLogsByIDHandler = ignoreLogsByIDHandler;
                _allowLogsByIDHandler = allowLogsByIDHandler;
                _statLogsByDateHandler = statLogsByDateHandler;
                _registerGuidToMemberLogic = registerGuidToMemberLogic;
                _removeGuidFromMemberLogic = removeGuidFromMemberLogic;
                _lastStatLogsHandler = lastStatLogsHandler;
                _tagLogToMatchHandler = tagLogToMatchHandler;
                _unTagLogToMatchHandler = unTagLogToMatchHandler;
                _logger = logger;
            }

            [SlashCommand("register_guid", "Register a GUID to a Member's profile")]
            public async Task RegisterGuidToMemberAsync(
                [Summary("member", "The member to register the GUID to")] IUser member,
                [Summary("guid", "The GUID to register")] string guid)
            {
                try
                {
                    await DeferAsync(ephemeral: true);
                    var result = await _registerGuidToMemberLogic.RegisterGuidToMemberProcess(member, guid);
                    await FollowupAsync(embed: result, ephemeral: true);
                    _autocompleteCache.Update();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Command error in {Command}.", nameof(RegisterGuidToMemberAsync));
                    await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }

            [SlashCommand("remove_guid", "Remove a GUID from a Member's profile")]
            public async Task RemoveGuidFromMemberAsync(
                [Summary("member", "The member to remove the GUID from")] IUser member,
                [Summary("guid", "The GUID to remove"), Autocomplete(typeof(MemberUT2004GuidAutocomplete))] string guid)
            {
                try
                {
                    await DeferAsync(ephemeral: true);
                    var result = await _removeGuidFromMemberLogic.RemoveGuidFromMemberProcess(member, guid);
                    await FollowupAsync(embed: result, ephemeral: true);
                    _autocompleteCache.Update();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Command error in {Command}.", nameof(RemoveGuidFromMemberAsync));
                    await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }

            [SlashCommand("get_log", "Get up to 10 different logs by their ID# DMed to the user")]
            public async Task GetLogByIdAsync(string firstLog, string secondLog = null, string thirdLog = null, string fourthLog = null, string fifthLog = null, string sixthLog = null, string seventhLog = null, string eighthLog = null, string ninthLog = null, string tenthLog = null)
            {
                try
                {
                    await DeferAsync(ephemeral: true);
                    var logIds = new List<string> { firstLog, secondLog, thirdLog, fourthLog, fifthLog, sixthLog, seventhLog, eighthLog, ninthLog, tenthLog }
                                 .Where(id => !string.IsNullOrWhiteSpace(id))
                                 .ToList();

                    var result = await _getLogsByIDHandler.GetLogsByID(Context, logIds);
                    await FollowupAsync(result, ephemeral: true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Command error in {Command}.", nameof(GetLogByIdAsync));
                    await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }

            [SlashCommand("logs_by_date", "Get all stat log ID#'s for a specific day")]
            public async Task StatLogsByDateAsync(int year, int month, int day, string serverName = null)
            {
                try
                {
                    await DeferAsync(ephemeral: true);
                    if (year < 2000 || year > DateTime.Now.Year) { await FollowupAsync("Invalid year.", ephemeral: true); return; }
                    if (month < 1 || month > 12) { await FollowupAsync("Month must be 1-12.", ephemeral: true); return; }
                    if (day < 1 || day > DateTime.DaysInMonth(year, month)) { await FollowupAsync("Invalid day for the given month/year.", ephemeral: true); return; }

                    var date = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
                    var result = await _statLogsByDateHandler.GetStatLogsByDate(date, serverName);

                    if (string.IsNullOrWhiteSpace(result))
                        await FollowupAsync("No stat logs found for that date.", ephemeral: true);
                    else
                        await FollowupAsync(result, ephemeral: true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Command error in {Command}.", nameof(StatLogsByDateAsync));
                    await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }

            [SlashCommand("last_logs", "Get the last 1 to 25 compiled StatLog ID's")]
            public async Task LastStatLogAsync(int amount, string serverName = null)
            {
                try
                {
                    await DeferAsync(ephemeral: true);
                    if (amount < 1 || amount > 25)
                    {
                        await FollowupAsync("Amount must be between 1 and 25.", ephemeral: true);
                        return;
                    }

                    var result = await _lastStatLogsHandler.GetLastStatLogsProcess(amount, serverName);
                    if (string.IsNullOrWhiteSpace(result))
                        await FollowupAsync("No stat logs found.", ephemeral: true);
                    else
                        await FollowupAsync(result, ephemeral: true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Command error in {Command}.", nameof(LastStatLogAsync));
                    await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }

            [SlashCommand("ignore_logs", "Ignore up to 10 specific logs by their ID# so they won't be processed for stats")]
            public async Task IgnoreLogsByIdAsync(string firstLog, string secondLog = null, string thirdLog = null, string fourthLog = null, string fifthLog = null, string sixthLog = null, string seventhLog = null, string eighthLog = null, string ninthLog = null, string tenthLog = null)
            {
                try
                {
                    await DeferAsync(ephemeral: true);
                    var logIds = new List<string> { firstLog, secondLog, thirdLog, fourthLog, fifthLog, sixthLog, seventhLog, eighthLog, ninthLog, tenthLog }
                                 .Where(id => !string.IsNullOrWhiteSpace(id))
                                 .ToList();

                    var result = await _ignoreLogsByIDHandler.IgnoreLogsByIDProcess(Context, logIds);
                    await FollowupAsync(embed: result, ephemeral: true);
                    _autocompleteCache.Update();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Command error in {Command}.", nameof(IgnoreLogsByIdAsync));
                    await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }

            [SlashCommand("allow_logs", "Re-allow up to 10 previously ignored logs by their ID# so they count towards stats again")]
            public async Task AllowLogsByIdAsync(
                [Summary("first_log", "The first log ID to ignore"), Autocomplete(typeof(AdminAllowLogsAutocomplete))] string firstLog,
                [Summary("second_log", "The second log ID to ignore"), Autocomplete(typeof(AdminAllowLogsAutocomplete))] string secondLog = null,
                [Summary("third_log", "The third log ID to ignore"), Autocomplete(typeof(AdminAllowLogsAutocomplete))] string thirdLog = null,
                [Summary("fourth_log", "The fourth log ID to ignore"), Autocomplete(typeof(AdminAllowLogsAutocomplete))] string fourthLog = null,
                [Summary("fifth_log", "The fifth log ID to ignore"), Autocomplete(typeof(AdminAllowLogsAutocomplete))] string fifthLog = null,
                [Summary("sixth_log", "The sixth log ID to ignore"), Autocomplete(typeof(AdminAllowLogsAutocomplete))] string sixthLog = null,
                [Summary("seventh_log", "The seventh log ID to ignore"), Autocomplete(typeof(AdminAllowLogsAutocomplete))] string seventhLog = null,
                [Summary("eighth_log", "The eighth log ID to ignore"), Autocomplete(typeof(AdminAllowLogsAutocomplete))] string eighthLog = null,
                [Summary("ninth_log", "The ninth log ID to ignore"), Autocomplete(typeof(AdminAllowLogsAutocomplete))] string ninthLog = null,
                [Summary("tenth_log", "The tenth log ID to ignore"), Autocomplete(typeof(AdminAllowLogsAutocomplete))] string tenthLog = null)
            {
                try
                {
                    await DeferAsync(ephemeral: true);
                    var logIds = new List<string> { firstLog, secondLog, thirdLog, fourthLog, fifthLog, sixthLog, seventhLog, eighthLog, ninthLog, tenthLog }
                                 .Where(id => !string.IsNullOrWhiteSpace(id))
                                 .ToList();

                    var result = await _allowLogsByIDHandler.AllowLogsByIDProcess(Context, logIds);
                    await FollowupAsync(embed: result, ephemeral: true);
                    _autocompleteCache.Update();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Command error in {Command}.", nameof(AllowLogsByIdAsync));
                    await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }

            [SlashCommand("tag_log", "Tag a log to a post-match in a tournament for easier reference")]
            public async Task TagLogToMatchAsync(
                [Summary("log_id", "The log ID to tag to a match")] string logId,
                [Summary("tournament_id", "The tournament ID of the match to tag the log to"), Autocomplete(typeof(TournamentIdAutocomplete))] string tournamentId,
                [Summary("match_id", "The match ID of the match to tag the log to"), Autocomplete(typeof(TagLogToMatchAutocomplete))] string matchId)
            {
                try
                {
                    await DeferAsync(ephemeral: true);
                    var result = await _tagLogToMatchHandler.TagLogToMatchProcess(logId, tournamentId, matchId);
                    await FollowupAsync(embed: result, ephemeral: true);
                    _autocompleteCache.Update();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Command error in {Command}.", nameof(TagLogToMatchAsync));
                    await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }

            [SlashCommand("untag_log", "Remove a log's tag from a post-match in a tournament")]
            public async Task UnTagLogToMatchAsync(
                [Summary("log_id", "The log ID to untag from a match")] string logId)
            {
                try
                {
                    await DeferAsync(ephemeral: true);
                    var result = await _unTagLogToMatchHandler.UnTagLogFromMatchProcess(logId);
                    await FollowupAsync(embed: result, ephemeral: true);
                    _autocompleteCache.Update();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Command error in {Command}.", nameof(UnTagLogToMatchAsync));
                    await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }
        }
    }
}