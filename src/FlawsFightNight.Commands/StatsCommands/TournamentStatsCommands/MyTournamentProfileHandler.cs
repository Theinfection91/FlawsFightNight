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
    public class MyTournamentProfileHandler : CommandHandler
    {
        private readonly EmbedFactory _embedFactory;
        private readonly MemberService _memberService;
        public MyTournamentProfileHandler(EmbedFactory embedFactory, MemberService memberService) : base("My Tournament Profile")
        {
            _embedFactory = embedFactory;
            _memberService = memberService;
        }

        public async Task<Embed> MyTournamentProfileProcess(SocketInteractionContext context)
        {
            var memberProfile = _memberService.GetMemberProfile(context.User.Id);

            return _embedFactory.GenericEmbed(Name, memberProfile?.GetAllStats()!, Color.DarkGreen);
        }
    }
}