using Discord;
using Discord.Interactions;
using FlawsFightNight.CommandsLogic.MatchCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Bot.SlashCommands
{
    [Group("match", "Commands related to matches like reporting who won, admin editing, challenges for ladders, etc.")]
    public class MatchCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private EditMatchLogic _editMatchLogic;
        private ReportWinLogic _reportWinLogic;

        public MatchCommands(EditMatchLogic editMatchLogic, ReportWinLogic reportWinLogic)
        {
            _editMatchLogic = editMatchLogic;
            _reportWinLogic = reportWinLogic;
        }

        [SlashCommand("report-win", "Report a win of any kind of tournament.")]
        public async Task ReportWinAsync(
            [Summary("match_id", "The ID of the match to target."), Autocomplete] string matchId,
            [Summary("winning_team_name", "The name of the winning team."), Autocomplete] string winningTeamName,
            [Summary("winning_team_score", "The score of the winning team")] int winningTeamScore,
            [Summary("losing_team_score", "The score of the losing team")] int losingTeamScore)
        {
            try
            {
                await DeferAsync();
                var result = _reportWinLogic.ReportWinProcess(Context, matchId, winningTeamName, winningTeamScore, losingTeamScore);
                await FollowupAsync(embed: result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Command Error: {ex}");
                await RespondAsync("An error occurred while processing this command.", ephemeral: true);
            }
        }

        [SlashCommand("edit", "Edit a post-match's details in RR and Elimination.")]
        public async Task EditMatchAsync(
            [Summary("post_match_id", "The ID of the match to target."), Autocomplete] string matchId,
            [Summary("winning_team_name", "The winner of the match, can be the same as before edit."), Autocomplete] string winningTeamName,
            [Summary("winning_team_score", "The score of the winning team.")] int winningTeamScore,
            [Summary("losing_team_score", "The score of the losing team")] int losingTeamScore)
        {
            try
            {
                var result = _editMatchLogic.EditMatchProcess(matchId, winningTeamName, winningTeamScore, losingTeamScore);
                await RespondAsync(embed: result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Command Error: {ex}");
                await RespondAsync("An error occurred while processing this command.", ephemeral: true);
            }
        }

        [Group("challenge", "Challenge related match commands for ladder tournaments.")]
        public class MatchesChannelCommands : InteractionModuleBase<SocketInteractionContext>
        {
            private SendChallengeLogic _sendChallengeLogic;
            private CancelChallengeLogic _cancelChallengeLogic;
            public MatchesChannelCommands(SendChallengeLogic sendChallengeLogic, CancelChallengeLogic cancelChallengeLogic)
            {
                _sendChallengeLogic = sendChallengeLogic;
                _cancelChallengeLogic = cancelChallengeLogic;
            }
            [SlashCommand("send", "Send a challenge to another team in a ladder tournament.")]
            public async Task SendChallengeAsync(
            [Summary("challenger_team_name", "The name of the team sending the challenge"), Autocomplete] string challengerTeamName,
            [Summary("challenged_team", "The name of the team being challenged"), Autocomplete] string opponentTeamName)
            {
                try
                {
                    var result = _sendChallengeLogic.SendChallengeProcess(Context, challengerTeamName, opponentTeamName);
                    await RespondAsync(embed: result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Command Error: {ex}");
                    await RespondAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }
            [SlashCommand("cancel", "Cancel a previously sent challenge in a ladder tournament.")]
            public async Task CancelChallengeAsync(
            [Summary("challenger_team", "The name of the team that sent the challenge"), Autocomplete] string challengerTeamName)
            {
                try
                {
                    var result = _cancelChallengeLogic.CancelChallengeProcess(Context, challengerTeamName);
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
