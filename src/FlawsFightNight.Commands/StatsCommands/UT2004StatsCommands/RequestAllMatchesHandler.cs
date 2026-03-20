using Discord;
using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.StatsCommands.UT2004StatsCommands
{
    public class RequestAllMatchesHandler : CommandHandler
    {
        private readonly EmbedFactory _embedFactory;
        private readonly MemberService _memberService;
        private readonly UT2004StatsService _ut2004StatsService;

        public RequestAllMatchesHandler(EmbedFactory embedFactory, MemberService memberService, UT2004StatsService ut2004StatsService) : base("Request All Matches")
        {
            _embedFactory = embedFactory;
            _memberService = memberService;
            _ut2004StatsService = ut2004StatsService;
        }

        public async Task<(Embed embed, string? fileContent, string? fileName)> Handle(ulong discordId)
        {
            var memberProfile = _memberService.GetMemberProfile(discordId);
            if (memberProfile == null || memberProfile.RegisteredUT2004GUIDs.Count == 0)
            {
                return (_embedFactory.ErrorEmbed(Name, "You don't have a UT2004 GUID registered. Use `/stats ut2004 register_guid` to link your profile."), null, null);
            }

            var results = await _ut2004StatsService.GetAllStatLogIdsByGuids(memberProfile.RegisteredUT2004GUIDs);

            if (results.Count == 0)
            {
                return (_embedFactory.ErrorEmbed(Name, "No matches found for your registered GUID(s). Stats may not have been processed yet."), null, null);
            }

            var sb = new StringBuilder();
            sb.AppendLine($"All Matches for Discord ID: {discordId}");
            sb.AppendLine($"Registered GUIDs: {string.Join(", ", memberProfile.RegisteredUT2004GUIDs)}");
            sb.AppendLine($"Total Matches Found: {results.Count}");
            sb.AppendLine(new string('-', 60));
            foreach (var entry in results)
            {
                sb.AppendLine(entry);
            }

            var embed = _embedFactory.GenericEmbed(Name, $"Found **{results.Count}** match(es) for your registered GUID(s). Check your DMs for the full list!", Color.DarkBlue);
            return (embed, sb.ToString(), $"all_matches_{discordId}.txt");
        }
    }
}
