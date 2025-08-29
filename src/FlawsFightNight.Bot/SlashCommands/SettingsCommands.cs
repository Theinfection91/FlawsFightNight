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
        private RemoveDebugAdminLogic _removeDebugAdminLogic;
        public SettingsCommands(AddDebugAdminLogic addDebugAdminLogic, RemoveDebugAdminLogic removeDebugAdminLogic)
        {
            _addDebugAdminLogic = addDebugAdminLogic;
            _removeDebugAdminLogic = removeDebugAdminLogic;
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

        [SlashCommand("remove_debug_admin", "Remove a user from debug admins")]
        [RequireGuildAdmin]
        public async Task RemoveDebugAdminAsync(
            [Summary("user", "The user to remove from debug admins")] IUser user)
        {
            try
            {
                var result = _removeDebugAdminLogic.RemoveDebugAdminProcess(user.Id);
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
            private RemoveMatchesChannelLogic _removeMatchesChannelLogic;
            public MatchesChannelCommands(SetMatchesChannelLogic setMatchesChannelLogic, RemoveMatchesChannelLogic removeMatchesChannelLogic)
            {
                _setMatchesChannelLogic = setMatchesChannelLogic;
                _removeMatchesChannelLogic = removeMatchesChannelLogic;
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
            public async Task RemoveMatchesChannelIdAsync(
            [Summary("tournament_id", "The ID of the tournament to stop the matches LiveView.")] string tournamentId)
            {
                try
                {
                    var result = _removeMatchesChannelLogic.RemoveMatchesChannelProcess(tournamentId);
                    await RespondAsync(embed: result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Command Error: {ex}");
                    await RespondAsync("An error occurred while processing this command.", ephemeral: true);
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
            [RequireGuildAdmin]
            public async Task SetStandingsChannelIdAsync(
            [Summary("tournament_id", "The ID of the tournament to set the standings channel for")] string tournamentId,
            [Summary("channel_id", "The ID of the channel where standings will be posted")] IMessageChannel channel)
            {

                try
                {
                    var result = _setStandingsChannelLogic.SetStandingsChannelProcess(tournamentId, channel);
                    await RespondAsync(embed: result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Command Error: {ex}");
                    await RespondAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }

            [SlashCommand("remove", "Remove the channel ID for standings of a specified tournament")]
            [RequireGuildAdmin]
            public async Task RemoveStandingsChannelIdAsync(
            [Summary("tournament_id", "The ID of the tournament to stop the standings LiveView.")] string tournamentId)
            {
                try
                {

                    var result = _removeStandingsChannelLogic.RemoveStandingsChannelProcess(tournamentId);
                    await RespondAsync(embed: result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Command Error: {ex}");
                    await RespondAsync("An error occurred while processing this command.", ephemeral: true);
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
            [RequireGuildAdmin]
            public async Task SetTeamsChannelIdAsync(
            [Summary("tournament_id", "The ID of the tournament to set the teams channel for")] string tournamentId,
            [Summary("channel_id", "The ID of the channel where teams will be posted")] IMessageChannel channel)
            {
                try
                {
                    var result = _setTeamsChannelLogic.SetTeamsChannelProcess(tournamentId, channel);
                    await RespondAsync(embed: result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Command Error: {ex}");
                    await RespondAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }
            [SlashCommand("remove", "Remove the channel ID for teams of a specified tournament")]
            [RequireGuildAdmin]
            public async Task RemoveTeamsChannelIdAsync(
            [Summary("tournament_id", "The ID of the tournament to stop the teams LiveView.")] string tournamentId)
            {
                try
                {
                    var result = _removeTeamsChannelLogic.RemoveTeamsChannelProcess(tournamentId);
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
}
