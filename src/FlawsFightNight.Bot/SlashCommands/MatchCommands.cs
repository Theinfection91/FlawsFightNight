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
        private CancelChallengeLogic _cancelChallengeLogic;
        private EditMatchLogic _editMatchLogic;
        private SendChallengeLogic _sendChallengeLogic;

        public MatchCommands(CancelChallengeLogic cancelChallengeLogic, EditMatchLogic editMatchLogic, SendChallengeLogic sendChallengeLogic)
        {
            _cancelChallengeLogic = cancelChallengeLogic;
            _editMatchLogic = editMatchLogic;
            _sendChallengeLogic = sendChallengeLogic;
        }

        [Group("report-win", "Report a win for a team in a match")]
        public class ReportWinCommands : InteractionModuleBase<SocketInteractionContext>
        {
            private ReportRoundRobinWinLogic _reportWinLogic;
            public ReportWinCommands(ReportRoundRobinWinLogic reportWinLogic)
            {
                _reportWinLogic = reportWinLogic;
            }
            [SlashCommand("round-robin", "Report a round robin win")]
            public async Task ReportRoundRobinWinAsync(
            [Summary("match_id", "The ID of the match to target.")] string matchId,
            [Summary("winning_team_name", "The name of the winning team.")] string winningTeamName,
            [Summary("winning_team_score", "The score of the winning team")] int winningTeamScore,
            [Summary("losing_team_score", "The score of the losing team")] int losingTeamScore)
            {
                try
                {
                    var result = _reportWinLogic.ReportRoundRobinWinProcess(Context, matchId, winningTeamName, winningTeamScore, losingTeamScore);
                    await RespondAsync(embed: result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Command Error: {ex}");
                    await RespondAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }
        }
        
        [SlashCommand("edit", "Edit a post-match's details in RR and Elimination.")]
        public async Task EditMatchAsync(
            [Summary("match_id", "The ID of the match to target.")] string matchId,
            [Summary("winningTeamName", "The winner of the match, can be the same as before edit.")] string winningTeamName,
            [Summary("winningTeamScore", "The score of the winning team.")] int winningTeamScore,
            [Summary("losingTeamScore", "The score of the losing team")] int losingTeamScore)
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

        //[Group("challenge", "Challenge related match commands for ladder tournaments.")]
        //public class MatchesChannelCommands : InteractionModuleBase<SocketInteractionContext>
        //{
        //    private SendChallengeLogic _sendChallengeLogic;
        //    private CancelChallengeLogic _cancelChallengeLogic;
        //    public MatchesChannelCommands(SendChallengeLogic sendChallengeLogic, CancelChallengeLogic cancelChallengeLogic)
        //    {
        //        _sendChallengeLogic = sendChallengeLogic;
        //        _cancelChallengeLogic = cancelChallengeLogic;
        //    }
        //    [SlashCommand("send", "Send a challenge to another team in a ladder tournament.")]
        //    public async Task SendChallengeAsync(
        //    [Summary("challenger_team_name", "The name of the team sending the challenge")] string challengerTeamName,
        //    [Summary("challenged_team", "The name of the team being challenged")] string opponentTeamName)
        //    {
        //        try
        //        {
        //            //var result = ;
        //            //await RespondAsync(embed: result);
        //            await RespondAsync("Challenges and Ladder Tournaments coming soon.", ephemeral: true);
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"Command Error: {ex}");
        //            await RespondAsync("An error occurred while processing this command.", ephemeral: true);
        //        }
        //    }
        //    [SlashCommand("cancel", "Cancel a previously sent challenge in a ladder tournament.")]
        //    public async Task CancelChallengeAsync(
        //    [Summary("challenger_team_name", "The name of the team that sent the challenge")] string challengerTeamName)
        //    {
        //        try
        //        {
        //            //var result = ;
        //            //await RespondAsync(embed: result);
        //            await RespondAsync("Challenges and Ladder Tournaments coming soon.", ephemeral: true);
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"Command Error: {ex}");
        //            await RespondAsync("An error occurred while processing this command.", ephemeral: true);
        //        }
        //    }
        //}
    }
}
