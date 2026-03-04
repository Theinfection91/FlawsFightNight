using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using FlawsFightNight.Bot.Autocomplete;
using FlawsFightNight.Bot.Components;
using FlawsFightNight.Bot.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlawsFightNight.Commands.SettingsCommands;

namespace FlawsFightNight.Bot.SlashCommands
{
    [Group("settings", "Commands for tournament and admin settings")]
    [RequireGuildAdmin]
    public class SettingsCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private AddDebugAdminLogic _addDebugAdminLogic;
        private RemoveDebugAdminLogic _removeDebugAdminLogic;
        public SettingsCommands(AddDebugAdminLogic addDebugAdminLogic, RemoveDebugAdminLogic removeDebugAdminLogic)
        {
            _addDebugAdminLogic = addDebugAdminLogic;
            _removeDebugAdminLogic = removeDebugAdminLogic;
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
                Console.WriteLine($"Command Error: {ex}");
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
                Console.WriteLine($"Command Error: {ex}");
                await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
            }
        }
        #endregion

        [Group("matches_channel_id", "Set or remove the channel ID for matches of a specified tournament")]
        public class MatchesChannelCommands : InteractionModuleBase<SocketInteractionContext>
        {
            private SetMatchesChannelLogic _setMatchesChannelLogic;
            private RemoveMatchesChannelLogic _removeMatchesChannelLogic;
            public MatchesChannelCommands(SetMatchesChannelLogic setMatchesChannelLogic, RemoveMatchesChannelLogic removeMatchesChannelLogic)
            {
                _setMatchesChannelLogic = setMatchesChannelLogic;
                _removeMatchesChannelLogic = removeMatchesChannelLogic;
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
                    Console.WriteLine($"Command Error: {ex}");
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
                    Console.WriteLine($"Command Error: {ex}");
                    await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }
        }

        [Group("standings_channel_id", "Set or remove the channel ID for standings of a specified tournament")]
        public class StandingsChannelCommands : InteractionModuleBase<SocketInteractionContext>
        {
            private SetStandingsChannelLogic _setStandingsChannelLogic;
            private RemoveStandingsChannelLogic _removeStandingsChannelLogic;

            public StandingsChannelCommands(SetStandingsChannelLogic setStandingsChannelLogic, RemoveStandingsChannelLogic removeStandingsChannelLogic)
            {
                _setStandingsChannelLogic = setStandingsChannelLogic;
                _removeStandingsChannelLogic = removeStandingsChannelLogic;
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
                    Console.WriteLine($"Command Error: {ex}");
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
                    Console.WriteLine($"Command Error: {ex}");
                    await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }
        }

        [Group("teams_channel_id", "Set or remove the channel ID for teams of a specified tournament")]
        public class TeamsChannelCommands : InteractionModuleBase<SocketInteractionContext>
        {
            private SetTeamsChannelLogic _setTeamsChannelLogic;
            private RemoveTeamsChannelLogic _removeTeamsChannelLogic;
            public TeamsChannelCommands(SetTeamsChannelLogic setTeamsChannelLogic, RemoveTeamsChannelLogic removeTeamsChannelLogic)
            {
                _setTeamsChannelLogic = setTeamsChannelLogic;
                _removeTeamsChannelLogic = removeTeamsChannelLogic;
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
                    Console.WriteLine($"Command Error: {ex}");
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
                    Console.WriteLine($"Command Error: {ex}");
                    await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }
        }

        [Group("ftp_stats_service", "Re-run FTP Setup Process in Console and remove FTP credentials")]
        public class FTPStatsServiceCommands : InteractionModuleBase<SocketInteractionContext>
        {
            private readonly AutocompleteCache _autocompleteCache;
            private RemoveFTPCredentialsLogic _removeFTPCredentialsLogic;
            public FTPStatsServiceCommands(AutocompleteCache autocompleteCache, RemoveFTPCredentialsLogic removeFTPCredentialsLogic)
            {
                _autocompleteCache = autocompleteCache;
                _removeFTPCredentialsLogic = removeFTPCredentialsLogic;
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
                    Console.WriteLine($"Command Error: {ex}");
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
                    Console.WriteLine($"Command Error: {ex}");
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
                    Console.WriteLine($"Command Error: {ex}");
                    await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }
        }

        [Group("ut2004", "Admin commands related to UT2004 data")]
        public class UT2004Commands : InteractionModuleBase<SocketInteractionContext>
        {
            private readonly AutocompleteCache _autocompleteCache;
            private readonly RegisterGuidToMemberLogic _registerGuidToMemberLogic;
            private readonly RemoveGuidFromMemberLogic _removeGuidFromMemberLogic;
            public UT2004Commands(AutocompleteCache autocompleteCache, RegisterGuidToMemberLogic registerGuidToMemberLogic, RemoveGuidFromMemberLogic removeGuidFromMemberLogic)
            {
                _autocompleteCache = autocompleteCache;
                _registerGuidToMemberLogic = registerGuidToMemberLogic;
                _removeGuidFromMemberLogic = removeGuidFromMemberLogic;
            }

            [SlashCommand("register_guid", "Register a GUID to a Member's profile")]
            public async Task RegisterGuidToMemberAsync([Summary("member", "The member to register the GUID to")] IUser member,
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
                    Console.WriteLine($"Command Error: {ex}");
                    await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }

            [SlashCommand("remove_guid", "Remove a GUID from a Member's profile")]
            public async Task RemoveGuidFromMemberAsync([Summary("member", "The member to remove the GUID from")] IUser member,
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
                    Console.WriteLine($"Command Error: {ex}");
                    await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }
        }
    }
}
