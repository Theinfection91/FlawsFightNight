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
        private ReportWinLogic _reportWinLogic;
        private SendChallengeLogic _sendChallengeLogic;

        public MatchCommands(CancelChallengeLogic cancelChallengeLogic, EditMatchLogic editMatchLogic, ReportWinLogic reportWinLogic, SendChallengeLogic sendChallengeLogic)
        {
            _cancelChallengeLogic = cancelChallengeLogic;
            _editMatchLogic = editMatchLogic;
            _reportWinLogic = reportWinLogic;
            _sendChallengeLogic = sendChallengeLogic;
        }

        [SlashCommand("report-win", "Report a win for a team in a match")]
        public async Task ReportWinAsync(
            [Summary("winning_team_name", "The name of the winning team.")] string winningTeamName,
            [Summary("winning_team_score", "The score of the winning team")] int winningTeamScore,
            [Summary("losing_team_score", "The score of the losing team")] int losingTeamScore)
        {
            try
            {
                var result = _reportWinLogic.ReportWinProcess(Context, winningTeamName, winningTeamScore, losingTeamScore);
                await RespondAsync(embed: result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Command Error: {ex}");
                await RespondAsync("An error occurred while processing this command.", ephemeral: true);
            }
        }

        [SlashCommand("edit", "Edit a post-match's details in RR and Elimination.")]
        public async Task EditMatchAsync(
            [Summary("tournament_id", "The ID of the tournament the match is in.")] string tournamentId,
            [Summary("team_reference", "The new details for the match.")] string newDetails)
        {
            try
            {


                //var result = ;
                //await RespondAsync(embed: result);
                await RespondAsync("TODO");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Command Error: {ex}");
                await RespondAsync("An error occurred while processing this command.", ephemeral: true);
            }
        }
    }
}
