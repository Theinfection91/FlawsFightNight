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
            private readonly MyTournamentProfileLogic _myTournamentProfileLogic;
            public TournamentStatsCommands(MyTournamentProfileLogic myTournamentProfileLogic)
            {
                _myTournamentProfileLogic = myTournamentProfileLogic;
            }

            [SlashCommand("my_profile", "Displays your tournament profile with statistics and achievements.")]
            public async Task MyTournamentProfileAsync()
            {
                await DeferAsync(ephemeral: true);
                var embed = await _myTournamentProfileLogic.MyTournamentProfileProcess(Context);
                await FollowupAsync(embed: embed, ephemeral: true);
            }
        }

        [Group("ut2004", "Commands related to UT2004 statistics.")]
        public class UT2004StatsCommands : InteractionModuleBase<SocketInteractionContext>
        {
            private readonly AutocompleteCache _autocompleteCache;
            private readonly MyPlayerProfileLogic _myPlayerProfileLogic;
            private readonly RegisterGuidLogic _registerGuidLogic;
            private readonly RemoveGuidLogic _removeGuidLogic;
            public UT2004StatsCommands(AutocompleteCache autocompleteCache, MyPlayerProfileLogic myPlayerProfileLogic, RegisterGuidLogic registerGuidLogic, RemoveGuidLogic removeGuidLogic)
            {
                _autocompleteCache = autocompleteCache;
                _myPlayerProfileLogic = myPlayerProfileLogic;
                _registerGuidLogic = registerGuidLogic;
                _removeGuidLogic = removeGuidLogic;
            }

            [SlashCommand("register_guid", "Registers a UT2004 GUID to link your player profile with your Discord account.")]
            public async Task RegisterGuidAsync(
                [Summary("guid", "The UT2004 GUID to register.")] string guid)
            {
                await DeferAsync(ephemeral: true);
                var embed = await _registerGuidLogic.RegisterGuidProcess(Context, guid);
                await FollowupAsync(embed: embed, ephemeral: true);
                _autocompleteCache.Update();
            }

            [SlashCommand("remove_guid", "Removes a UT2004 GUID from your account.")]
            public async Task RemoveGuidAsync(
                [Summary("guid", "The UT2004 GUID to remove.")] string guid)
            {
                await DeferAsync(ephemeral: true);
                var embed = await _removeGuidLogic.RemoveGuidProcess(Context, guid);
                await FollowupAsync(embed: embed, ephemeral: true);
                _autocompleteCache.Update();
            }

            [SlashCommand("my_player", "Displays your UT2004 player profile with statistics and achievements.")]
            public async Task MyPlayerProfileAsync()
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
        }
    }
}
