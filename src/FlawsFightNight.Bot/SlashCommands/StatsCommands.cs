using Discord.Interactions;
using FlawsFightNight.Bot.Autocomplete;
using FlawsFightNight.Bot.Components;
using FlawsFightNight.Bot.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlawsFightNight.Commands.StatsCommands.TournamentStatsCommands;
using FlawsFightNight.Commands.StatsCommands.UT2004StatsCommands;
using Discord;
using System.IO;

namespace FlawsFightNight.Bot.SlashCommands
{
    [RequireMemberProfile]
    [Group("stats", "Commands related to tournament and UT2004 statistics.")]
    public class StatsCommands : InteractionModuleBase<SocketInteractionContext>
    {

        public StatsCommands()
        {

        }

        [Group("tournament", "Commands related to tournament statistics.")]
        public class TournamentStatsCommands : InteractionModuleBase<SocketInteractionContext>
        {
            private readonly MyTournamentProfileHandler _myTournamentProfileLogic;
            public TournamentStatsCommands(MyTournamentProfileHandler myTournamentProfileLogic)
            {
                _myTournamentProfileLogic = myTournamentProfileLogic;
            }

            [SlashCommand("my_profile", "Displays your tournament profile with statistics and achievements.")]
            public async Task MyTournamentProfileAsync()
            {
                try
                {
                    await DeferAsync(ephemeral: true);
                    var embed = await _myTournamentProfileLogic.MyTournamentProfileProcess(Context);
                    await FollowupAsync(embed: embed, ephemeral: true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Command Error: {ex}");
                    await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }
        }

        [Group("ut2004", "Commands related to UT2004 statistics.")]
        public class UT2004StatsCommands : InteractionModuleBase<SocketInteractionContext>
        {
            private readonly AutocompleteCache _autocompleteCache;
            private readonly DisplayMatchSummaryHandler _displayMatchSummary;
            private readonly MyPlayerProfileHandler _myPlayerProfileLogic;
            private readonly RegisterGuidHandler _registerGuidLogic;
            private readonly RemoveGuidHandler _removeGuidLogic;
            public UT2004StatsCommands(AutocompleteCache autocompleteCache, DisplayMatchSummaryHandler displayMatchSummaryHandler, MyPlayerProfileHandler myPlayerProfileLogic, RegisterGuidHandler registerGuidLogic, RemoveGuidHandler removeGuidLogic)
            {
                _autocompleteCache = autocompleteCache;
                _displayMatchSummary = displayMatchSummaryHandler;
                _myPlayerProfileLogic = myPlayerProfileLogic;
                _registerGuidLogic = registerGuidLogic;
                _removeGuidLogic = removeGuidLogic;
            }

            [SlashCommand("register_guid", "Registers a UT2004 GUID to link your player profile with your Discord account.")]
            public async Task RegisterGuidAsync(
                [Summary("guid", "The UT2004 GUID to register.")] string guid)
            {
                try
                {
                    await DeferAsync(ephemeral: true);
                    var embed = await _registerGuidLogic.RegisterGuidProcess(Context, guid);
                    await FollowupAsync(embed: embed, ephemeral: true);
                    _autocompleteCache.Update();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Command Error: {ex}");
                    await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }

            [SlashCommand("remove_guid", "Removes a UT2004 GUID from your account.")]
            public async Task RemoveGuidAsync(
                [Summary("guid", "The UT2004 GUID to remove.")] string guid)
            {
                try
                {
                    await DeferAsync(ephemeral: true);
                    var embed = await _removeGuidLogic.RemoveGuidProcess(Context, guid);
                    await FollowupAsync(embed: embed, ephemeral: true);
                    _autocompleteCache.Update();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Command Error: {ex}");
                    await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }

            [SlashCommand("my_player", "Displays your UT2004 player profile with statistics and achievements.")]
            public async Task MyPlayerProfileAsync()
            {
                try
                {
                    await DeferAsync(ephemeral: true);
                    var (embed, hasProfile) = await _myPlayerProfileLogic.MyPlayerProfileProcess(Context.User.Id);

                    if (hasProfile)
                    {
                        var components = ComponentFactory.CreateUT2004ProfileSelectMenu(Context.User.Id);
                        await FollowupAsync(embed: embed, components: components.Build(), ephemeral: true);
                    }
                    else
                    {
                        await FollowupAsync(embed: embed, ephemeral: true);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Command Error: {ex}");
                    await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }

            [SlashCommand("leaderboard", "Displays the interactive UT2004 player leaderboard.")]
            public async Task UserLevelLeaderboardAsync()
            {
                try
                {
                    await DeferAsync(ephemeral: true);
                    //var result
                    //var components = ComponentFactory.CreateUT2004LeaderboardSelectMenu();
                    await FollowupAsync(//embed: result, components: components.Build(),
                        ephemeral: true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Command Error: {ex}");
                    await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }

            [SlashCommand("match_summary", "Displays a summary of match by stat log ID.")]
            public async Task DisplayMatchSummaryAsync(
                [Summary("stat_log_id", "The stat log ID of the match.")] string statLogId)
            {
                try
                {
                    await DeferAsync(ephemeral: true);
                    var (embed, fileContent, fileName) = await _displayMatchSummary.Handle(statLogId);

                    if (fileContent != null && fileName != null)
                    {
                        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));
                        await FollowupWithFileAsync(ms, fileName, embed: embed, ephemeral: true);
                    }
                    else
                    {
                        await FollowupAsync(embed: embed, ephemeral: true);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Command Error: {ex}");
                    await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }

            [SlashCommand("my_tournament_matches", "Display all log ID#'s for your tournament matches as long admins have tagged them.")]
            public async Task MyTournamentMatchesAsync()
            {
                try
                {
                    await DeferAsync(ephemeral: true);
                    //var result = await _myTournamentMatches.MyTournamentMatchesProcess(Context.User.Id);
                    await FollowupAsync(//embed: result,
                        ephemeral: true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Command Error: {ex}");
                    await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }

            [SlashCommand("request_all_matches", "Request all ID#'s that contain your GUID. Will be sent in a DM once ready")]
            public async Task RequestAllMatchesAsync()
            {
                try
                {
                    await DeferAsync(ephemeral: true);
                    //var result = await _requestAllMatches.RequestAllMatchesProcess(Context.User.Id);
                    await FollowupAsync(//embed: result,
                        ephemeral: true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Command Error: {ex}");
                    await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }

            [SlashCommand("compare", "Compares two UT2004 player profiles.")]
            public async Task ComparePlayersAsync(
                [Summary("player1", "The first player to compare.")] IUser player1,
                [Summary("player2", "The second player to compare.")] IUser player2)
            {
                try
                {
                    await DeferAsync(ephemeral: true);
                    //var result = await _comparePlayers.ComparePlayersProcess(player1, player2);
                    await FollowupAsync(//embed: result,
                        ephemeral: true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Command Error: {ex}");
                    await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }

            [SlashCommand("suggest_teams", "When given even number of players, suggests teams based on ratings. Handles 2v2 to 5v5.")]
            public async Task SuggestTeamsAsync(
                [Summary("player1", "The first player for matchmaking")] IUser firstPlayer,
                [Summary("player2", "The second player for matchmaking")] IUser secondPlayer,
                [Summary("player3", "The third player for matchmaking")] IUser thirdPlayer,
                [Summary("player4", "The fourth player for matchmaking")] IUser fourthPlayer,
                [Summary("player5", "The fifth player for matchmaking")] IUser fifthPlayer,
                [Summary("player6", "The sixth player for matchmaking")] IUser sixthPlayer,
                [Summary("player7", "The seventh player for matchmaking")] IUser seventhPlayer,
                [Summary("player8", "The eighth player for matchmaking")] IUser eighthPlayer,
                [Summary("player9", "The ninth player for matchmaking")] IUser ninthPlayer,
                [Summary("player10", "The tenth player for matchmaking")] IUser tenthPlayer)
            {
                try
                {
                    await DeferAsync(ephemeral: true);
                    var players = new List<IUser> { firstPlayer, secondPlayer, thirdPlayer, fourthPlayer, fifthPlayer, sixthPlayer, seventhPlayer, eighthPlayer, ninthPlayer, tenthPlayer }
                        .Where(p => p != null)
                        .ToList();
                    //var result = await _suggestTeams.ComparePlayersProcess(players);
                    await FollowupAsync(//embed: result,
                        ephemeral: true);
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
