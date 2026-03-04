using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.StatsCommands.TournamentStatsCommands
{
    public class MyTournamentProfileLogic : CommandHandler
    {
        private readonly EmbedFactory _embedFactory;
        private readonly MemberService _memberManager;
        public MyTournamentProfileLogic(EmbedFactory embedFactory, MemberService memberManager) : base("My Tournament Profile")
        {
            _embedFactory = embedFactory;
            _memberManager = memberManager;
        }

        public async Task<Embed> MyTournamentProfileProcess(SocketInteractionContext context)
        {
            var memberProfile = _memberManager.GetMemberProfile(context.User.Id);

            return _embedFactory.GenericEmbed("Test", memberProfile?.GetAllStats()!, Color.DarkGreen);
        }
    }
}