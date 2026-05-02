using Discord;
using FlawsFightNight.Commands;
using FlawsFightNight.Services;
using System.Linq;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.StatsCommands.UT2004StatsCommands
{
    public class MyPlayerProfileHandler : CommandHandler
    {
        private readonly EmbedFactory _embedFactory;
        private readonly MemberService _memberService;

        public MyPlayerProfileHandler(EmbedFactory embedFactory, MemberService memberService) : base("My Player Profile")
        {
            _embedFactory = embedFactory;
            _memberService = memberService;
        }

        public Task<(Embed embed, bool hasProfile)> MyPlayerProfileProcess(ulong discordId)
        {
            var memberProfile = _memberService.GetMemberProfile(discordId);
            if (memberProfile == null || memberProfile.RegisteredUT2004GUIDs.Count == 0)
            {
                return Task.FromResult<(Embed, bool)>((
                    _embedFactory.ErrorEmbed(Name, "You don't have a UT2004 GUID registered. Use `/stats ut2004 register_guid` to link your profile."),
                    false));
            }

            var guid = memberProfile.RegisteredUT2004GUIDs.First();
            var utProfile = _memberService.GetUT2004PlayerProfile(guid);
            if (utProfile == null)
            {
                return Task.FromResult<(Embed, bool)>((
                    _embedFactory.ErrorEmbed(Name, $"No UT2004 stats found for your registered GUID `{guid}`. Stats may not have been processed yet."),
                    false));
            }

            return Task.FromResult<(Embed, bool)>((_embedFactory.UT2004ProfileGeneralEmbed(utProfile), true));
        }
    }
}
