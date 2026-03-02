using Discord.Interactions;
using FlawsFightNight.CommandsLogic.StatsCommands.TournamentStatsCommands;
using FlawsFightNight.CommandsLogic.StatsCommands.UT2004StatsCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Bot.SlashCommands
{
    [Group("stats", "Commands related to tournament and UT2004 statistics.")]
    public class StatsCommands : InteractionModuleBase<SocketInteractionContext>
    {

        public StatsCommands()
        {

        }

        [Group("tournament", "Commands related to tournament statistics.")]
        public class TournamentStatsCommands : InteractionModuleBase<SocketInteractionContext>
        {
            private readonly MyTournamentProfileLogic _myTournamentProfileLogic;
            public TournamentStatsCommands(MyTournamentProfileLogic myTournamentProfileLogic)
            {
                _myTournamentProfileLogic = myTournamentProfileLogic;
            }

            [SlashCommand("my_profile", "Displays your tournament profile with statistics and achievements.")]
            public async Task MyTournamentProfileAsync()
            {
                await DeferAsync(ephemeral: true);
                var embed = await _myTournamentProfileLogic.MyTournamentProfileProcess(Context.User.Id);
                await FollowupAsync(embed: embed, ephemeral: true);
            }
        }

        [Group("ut2004", "Commands related to UT2004 statistics.")]
        public class UT2004StatsCommands : InteractionModuleBase<SocketInteractionContext>
        {
            private readonly MyPlayerProfileLogic _myPlayerProfileLogic;
            public UT2004StatsCommands(MyPlayerProfileLogic myPlayerProfileLogic)
            {
                _myPlayerProfileLogic = myPlayerProfileLogic;
            }

            [SlashCommand("my_player", "Displays your UT2004 player profile with statistics and achievements.")]
            public async Task MyPlayerProfileAsync()
            {
                await DeferAsync(ephemeral: true);
                var embed = await _myPlayerProfileLogic.MyPlayerProfileProcess(Context.User.Id);
                await FollowupAsync(embed: embed, ephemeral: true);
            }
        }
    }
}
