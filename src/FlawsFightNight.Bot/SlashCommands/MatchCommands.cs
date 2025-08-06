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
        private ReportWinLogic _reportWinLogic;

        public MatchCommands(ReportWinLogic reportWinLogic)
        {
            _reportWinLogic = reportWinLogic;
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
                await RespondAsync(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Command Error: {ex}");
                await RespondAsync("An error occurred while processing this command.", ephemeral: true);
            }
        }
    }
}
