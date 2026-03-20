using Discord;
using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.StatsCommands.UT2004StatsCommands
{
    public class MyTournamentMatchesHandler : CommandHandler
    {
        private readonly EmbedFactory _embedFactory;
        private readonly MemberService _memberService;
        private readonly UT2004StatsService _ut2004StatsService;

        public MyTournamentMatchesHandler(EmbedFactory embedFactory, MemberService memberService, UT2004StatsService ut2004StatsService) : base("My Tournament Matches")
        {
            _embedFactory = embedFactory;
            _memberService = memberService;
            _ut2004StatsService = ut2004StatsService;
        }

        public async Task<Embed> Handle(ulong discordId)
        {
            var memberProfile = _memberService.GetMemberProfile(discordId);
            if (memberProfile == null || memberProfile.RegisteredUT2004GUIDs.Count == 0)
            {
                return _embedFactory.ErrorEmbed(Name, "You don't have a UT2004 GUID registered. Use `/stats ut2004 register_guid` to link your profile.");
            }

            var results = await _ut2004StatsService.GetTournamentStatLogIdsByGuids(memberProfile.RegisteredUT2004GUIDs);

            if (results.Count == 0)
            {
                return _embedFactory.ErrorEmbed(Name, "No tournament-tagged matches found for your registered GUID(s). Admins must tag stat logs to tournament matches first.");
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Found **{results.Count}** tournament match(es):\n");
            foreach (var entry in results)
            {
                sb.AppendLine($"📋 `{entry}`");
            }

            return _embedFactory.GenericEmbed(Name, sb.ToString(), Color.DarkBlue);
        }
    }
}
