using Discord;
using FlawsFightNight.Core.Enums.UT2004;
using FlawsFightNight.Services;
using System.Linq;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.StatsCommands.UT2004StatsCommands
{
    public class EloTraceUserHandler : CommandHandler
    {
        private readonly EmbedFactory _embedFactory;
        private readonly MemberService _memberService;
        private readonly UT2004StatsService _ut2004StatsService;

        public EloTraceUserHandler(EmbedFactory embedFactory, MemberService memberService, UT2004StatsService ut2004StatsService) : base("My Elo Trace")
        {
            _embedFactory = embedFactory;
            _memberService = memberService;
            _ut2004StatsService = ut2004StatsService;
        }

        public async Task<Embed> Handle(ulong discordId, UT2004GameMode uT2004GameMode)
        {
            var memberProfile = _memberService.GetMemberProfile(discordId);
            if (memberProfile == null)
                return _embedFactory.ErrorEmbed(Name, "Profile not found. Contact admin.");

            if (memberProfile.RegisteredUT2004GUIDs.Count == 0)
                return _embedFactory.ErrorEmbed(Name, "No UT2004 GUIDs registered. Please register a GUID to use this command.");

            var guid = memberProfile.RegisteredUT2004GUIDs.First();
            var eloTrace = await _ut2004StatsService.GetPlayerEloTrace(guid, uT2004GameMode);
            await _ut2004StatsService.SendTextFileDM(discordId, $"EloTrace_{uT2004GameMode}_{guid}.txt", eloTrace);

            bool isSeamless = memberProfile.RegisteredUT2004GUIDs.Count >= 2;
            string description = isSeamless
                ? $"Elo trace has been sent to your DMs.\n🔗 *SeamlessRatings active — stats merged across {memberProfile.RegisteredUT2004GUIDs.Count} GUIDs.*"
                : "Elo trace has been sent to your DMs.";

            return _embedFactory.GenericEmbed(Name + " Success", description, Color.DarkBlue);
        }
    }
}
