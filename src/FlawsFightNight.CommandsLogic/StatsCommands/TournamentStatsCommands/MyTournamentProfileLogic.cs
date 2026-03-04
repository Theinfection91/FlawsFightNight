using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using FlawsFightNight.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.StatsCommands.TournamentStatsCommands
{
    public class MyTournamentProfileLogic : Logic
    {
        private readonly EmbedManager _embedManager;
        private readonly MemberManager _memberManager;
        public MyTournamentProfileLogic(EmbedManager embedManager, MemberManager memberManager) : base("My Tournament Profile")
        {
            _embedManager = embedManager;
            _memberManager = memberManager;
        }

        public async Task<Embed> MyTournamentProfileProcess(SocketInteractionContext context)
        {
            var memberProfile = _memberManager.GetMemberProfile(context.User.Id);

            return _embedManager.GenericEmbed("Test", memberProfile?.GetAllStats()!, Color.DarkGreen);
        }
    }
}