using Discord;
using Discord.Interactions;
using FlawsFightNight.Bot.Autocomplete;
using FlawsFightNight.Bot.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlawsFightNight.Commands.MatchCommands;

namespace FlawsFightNight.Bot.SlashCommands
{
    [Group("match", "Commands related to matches like reporting who won, admin editing, challenges for ladders, etc.")]
    public class MatchCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly AutocompleteCache _autocompleteCache;
        private EditMatchHandler _editMatchLogic;
        private ReportWinHandler _reportWinLogic;

        public MatchCommands(AutocompleteCache autocompleteCache, EditMatchHandler editMatchLogic, ReportWinHandler reportWinLogic)
        {
            _autocompleteCache = autocompleteCache;
            _editMatchLogic = editMatchLogic;
            _reportWinLogic = reportWinLogic;
        }

        [SlashCommand("report-win", "Report a win of any kind of tournament.")]
        public async Task ReportWinAsync(
            [Summary("match_id", "The ID of the match to target."), Autocomplete(typeof(MatchIdAutocomplete))] string matchId,
            [Summary("winning_team_name", "The name of the winning team."), Autocomplete(typeof(WinningTeamNameAutocomplete))] string winningTeamName,
            [Summary("winning_team_score", "The score of the winning team")] int winningTeamScore,
            [Summary("losing_team_score", "The score of the losing team")] int losingTeamScore)
        {
            try
            {
                await DeferAsync();
                var result = await _reportWinLogic.ReportWinProcess(Context, matchId, winningTeamName, winningTeamScore, losingTeamScore);
                await FollowupAsync(embed: result);
                _autocompleteCache.Update();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Command Error: {ex}");
                await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
            }
        }

        [RequireGuildAdmin]
        [SlashCommand("edit", "Edit a post-match's details in RR and Elimination.")]
        [RequireGuildAdmin]
        public async Task EditMatchAsync(
            [Summary("post_match_id", "The ID of the match to target."), Autocomplete(typeof(PostMatchIdAutocomplete))] string postMatchId,
            [Summary("winning_team_name", "The winner of the match, can be the same as before edit."), Autocomplete(typeof(WinningTeamNameAutocomplete))] string winningTeamName,
            [Summary("winning_team_score", "The score of the winning team.")] int winningTeamScore,
            [Summary("losing_team_score", "The score of the losing team")] int losingTeamScore)
        {
            try
            {
                await DeferAsync();
                var result = await _editMatchLogic.EditMatchProcess(postMatchId, winningTeamName, winningTeamScore, losingTeamScore);
                await FollowupAsync(embed: result);
                _autocompleteCache.Update();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Command Error: {ex}");
                await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
            }
        }

        [Group("challenge", "Challenge related match commands for ladder tournaments.")]
        public class MatchesChannelCommands : InteractionModuleBase<SocketInteractionContext>
        {
            private readonly AutocompleteCache _autocompleteCache;
            private SendChallengeHandler _sendChallengeLogic;
            private CancelChallengeHandler _cancelChallengeLogic;
            public MatchesChannelCommands(AutocompleteCache autocompleteCache, SendChallengeHandler sendChallengeLogic, CancelChallengeHandler cancelChallengeLogic)
            {
                _autocompleteCache = autocompleteCache;
                _sendChallengeLogic = sendChallengeLogic;
                _cancelChallengeLogic = cancelChallengeLogic;
            }
            [SlashCommand("send", "Send a challenge to another team in a ladder tournament.")]
            public async Task SendChallengeAsync(
            [Summary("challenger_team_name", "The name of the team sending the challenge"), Autocomplete(typeof(SendChallengerTeamAutocomplete))] string challengerTeamName,
            [Summary("challenged_team", "The name of the team being challenged"), Autocomplete(typeof(SendChallengedTeamAutocomplete))] string opponentTeamName)
            {
                try
                {
                    await DeferAsync();
                    var result = await _sendChallengeLogic.SendChallengeProcess(Context, challengerTeamName, opponentTeamName);
                    await FollowupAsync(embed: result);
                    _autocompleteCache.Update();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Command Error: {ex}");
                    await FollowupAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }
            [SlashCommand("cancel", "Cancel a previously sent challenge in a ladder tournament.")]
            public async Task CancelChallengeAsync(
            [Summary("challenger_team", "The name of the team that sent the challenge"), Autocomplete(typeof(CancelChallengeAutocomplete))] string challengerTeamName)
            {
                try
                {
                    await DeferAsync();
                    var result = await _cancelChallengeLogic.CancelChallengeProcess(Context, challengerTeamName);
                    await FollowupAsync(embed: result);
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
