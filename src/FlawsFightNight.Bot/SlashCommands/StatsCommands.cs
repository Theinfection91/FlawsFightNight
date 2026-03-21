using Discord.Interactions;
using FlawsFightNight.Bot.Autocomplete;
using FlawsFightNight.Bot.Components;
using FlawsFightNight.Bot.Attributes;
using FlawsFightNight.Core.Enums.UT2004;
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
            private readonly ComparePlayersHandler _comparePlayersHandler;
            private readonly DisplayMatchSummaryHandler _displayMatchSummary;
            private readonly GetWinProbabilityHandler _winProbabilityHandler;
            private readonly MyPlayerProfileHandler _myPlayerProfileLogic;
            private readonly MyTournamentMatchesHandler _myTournamentMatchesHandler;
            private readonly RegisterGuidHandler _registerGuidLogic;
            private readonly RemoveGuidHandler _removeGuidLogic;
            private readonly RequestAllMatchesHandler _requestAllMatchesHandler;
            private readonly SuggestTeamsHandler _suggestTeamsHandler;
            private readonly UserLevelLeaderboardHandler _leaderboardHandler;

            public UT2004StatsCommands(
                AutocompleteCache autocompleteCache,
                ComparePlayersHandler comparePlayersHandler,
                DisplayMatchSummaryHandler displayMatchSummaryHandler,
                GetWinProbabilityHandler winProbabilityHandler,
                MyPlayerProfileHandler myPlayerProfileLogic,
                MyTournamentMatchesHandler myTournamentMatchesHandler,
                RegisterGuidHandler registerGuidLogic,
                RemoveGuidHandler removeGuidLogic,
                RequestAllMatchesHandler requestAllMatchesHandler,
                SuggestTeamsHandler suggestTeamsHandler,
                UserLevelLeaderboardHandler leaderboardHandler)
            {
                _autocompleteCache = autocompleteCache;
                _comparePlayersHandler = comparePlayersHandler;
                _displayMatchSummary = displayMatchSummaryHandler;
                _winProbabilityHandler = winProbabilityHandler;
                _myPlayerProfileLogic = myPlayerProfileLogic;
                _myTournamentMatchesHandler = myTournamentMatchesHandler;
                _registerGuidLogic = registerGuidLogic;
                _removeGuidLogic = removeGuidLogic;
                _requestAllMatchesHandler = requestAllMatchesHandler;
                _suggestTeamsHandler = suggestTeamsHandler;
                _leaderboardHandler = leaderboardHandler;
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
                    var (embed, hasProfiles) = await _leaderboardHandler.Handle();

                    if (hasProfiles)
                    {
                        var components = ComponentFactory.CreateUT2004LeaderboardSelectMenu();
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
                    var (embed, fileContent, fileName) = await _myTournamentMatchesHandler.Handle(Context.User.Id);

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

            [SlashCommand("request_all_matches", "Request all ID#'s that contain your GUID. Will be sent in a DM once ready")]
            public async Task RequestAllMatchesAsync()
            {
                try
                {
                    await DeferAsync(ephemeral: true);
                    var embed = await _requestAllMatchesHandler.Handle(Context.User);
                    await FollowupAsync(embed: embed, ephemeral: true);
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
                    var (embed, hasBothProfiles) = await _comparePlayersHandler.Handle(player1, player2);

                    if (hasBothProfiles)
                    {
                        var components = ComponentFactory.CreateUT2004CompareSelectMenu(player1.Id, player2.Id);
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

            [SlashCommand("suggest_teams", "When given even number of players, suggests balanced teams based on ratings. Handles 2v2 to 5v5.")]
            public async Task SuggestTeamsAsync(
                [Summary("game_mode", "The game mode to base team ratings on.")]
                [Choice("iCTF", 1)]
                [Choice("TAM", 2)]
                [Choice("iBR", 3)]
                [Choice("General", 4)] int gameMode,
                [Summary("player1", "The first player for matchmaking")] IUser firstPlayer,
                [Summary("player2", "The second player for matchmaking")] IUser secondPlayer,
                [Summary("player3", "The third player for matchmaking")] IUser thirdPlayer,
                [Summary("player4", "The fourth player for matchmaking")] IUser fourthPlayer,
                [Summary("player5", "The fifth player for matchmaking")] IUser fifthPlayer = null,
                [Summary("player6", "The sixth player for matchmaking")] IUser sixthPlayer = null,
                [Summary("player7", "The seventh player for matchmaking")] IUser seventhPlayer = null,
                [Summary("player8", "The eighth player for matchmaking")] IUser eighthPlayer = null,
                [Summary("player9", "The ninth player for matchmaking")] IUser ninthPlayer = null,
                [Summary("player10", "The tenth player for matchmaking")] IUser tenthPlayer = null)
            {
                try
                {
                    await DeferAsync(ephemeral: true);
                    var players = new List<IUser> { firstPlayer, secondPlayer, thirdPlayer, fourthPlayer, fifthPlayer, sixthPlayer, seventhPlayer, eighthPlayer, ninthPlayer, tenthPlayer }
                        .Where(p => p != null)
                        .ToList();
                    var mode = gameMode == 4 ? UT2004GameMode.Unknown : (UT2004GameMode)gameMode;
                    var embed = await _suggestTeamsHandler.Handle(players, mode);
                    await FollowupAsync(embed: embed, ephemeral: true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Command Error: {ex}");
                    await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }

            [SlashCommand("win_probability", "Calculates win probability between two tournament teams based on their UT2004 OpenSkill ratings.")]
            public async Task GetWinProbabilityAsync(
                [Summary("game_mode", "The game mode to base ratings on.")]
                [Choice("iCTF", 1)]
                [Choice("TAM", 2)]
                [Choice("iBR", 3)]
                [Choice("General", 4)] int gameMode,
                [Summary("team_one", "The first tournament team.")]
                [Autocomplete(typeof(UT2004WinProbTeamAutocomplete))] string teamOne,
                [Summary("team_two", "The second tournament team.")]
                [Autocomplete(typeof(UT2004WinProbTeamAutocomplete))] string teamTwo)
            {
                try
                {
                    await DeferAsync(ephemeral: true);
                    var mode = gameMode == 4 ? UT2004GameMode.Unknown : (UT2004GameMode)gameMode;
                    var embed = await _winProbabilityHandler.Handle(teamOne, teamTwo, mode);
                    await FollowupAsync(embed: embed, ephemeral: true);
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
