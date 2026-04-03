using Discord;
using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.SettingsCommands.UT2004AdminCommands
{
    internal class EloTraceAdminHandler : CommandHandler
    {
        private readonly EmbedFactory _embedFactory;
        private readonly MemberService _memberService;
        private readonly UT2004StatsService _ut2004StatsService;

        public EloTraceAdminHandler(EmbedFactory embedFactory, MemberService memberService, UT2004StatsService ut2004StatsService) : base("Admin Elo Trace")
        {
            _embedFactory = embedFactory;
            _memberService = memberService;
            _ut2004StatsService = ut2004StatsService;
        }

        public async Task<Embed> Handle()
        {
            return null;
        }
    }
}
