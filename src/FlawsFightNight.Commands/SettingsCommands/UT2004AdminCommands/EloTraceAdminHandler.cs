using Discord;
using FlawsFightNight.Core.Enums.UT2004;
using FlawsFightNight.Core.Helpers.UT2004;
using FlawsFightNight.Services;
using System.Linq;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.SettingsCommands.UT2004AdminCommands
{
    public class EloTraceAdminHandler : CommandHandler
    {
        private readonly EmbedFactory _embedFactory;
        private readonly MemberService _memberService;
        private readonly UT2004StatsService _ut2004StatsService;
        private readonly SeamlessRatingsMapper _ratingsMapper;

        public EloTraceAdminHandler(EmbedFactory embedFactory, MemberService memberService, UT2004StatsService ut2004StatsService, SeamlessRatingsMapper ratingsMapper) : base("Admin Elo Trace")
        {
            _embedFactory = embedFactory;
            _memberService = memberService;
            _ut2004StatsService = ut2004StatsService;
            _ratingsMapper = ratingsMapper;
        }

        public async Task<Embed> Handle(ulong discordId, string guid, UT2004GameMode uT2004GameMode)
        {
            string resolvedGuid = _ratingsMapper.Resolve(guid);
            bool inputWasAlias = !resolvedGuid.Equals(guid, StringComparison.OrdinalIgnoreCase);

            if (!_ut2004StatsService.IsGuidInDatabase(resolvedGuid) && !_ut2004StatsService.IsGuidInDatabase(guid))
                return _embedFactory.ErrorEmbed(Name, $"The provided GUID `{guid}` was not found in the database.");

            // Collect all alias GUIDs for the resolved primary regardless of which GUID was entered
            var aliases = _ratingsMapper.GetAliasesForPrimary(resolvedGuid).ToList();
            bool hasAliases = aliases.Count > 0;

            var eloTrace = await _ut2004StatsService.GetPlayerEloTrace(guid, uT2004GameMode);

            // Always include all known GUIDs in the filename when SeamlessRatings is active
            string fileName = hasAliases
                ? $"EloTrace_{uT2004GameMode}_{resolvedGuid}_aliases_{string.Join("_", aliases)}.txt"
                : $"EloTrace_{uT2004GameMode}_{resolvedGuid}.txt";

            await _ut2004StatsService.SendTextFileDM(discordId, fileName, eloTrace);

            string description = hasAliases
                ? $"Elo trace has been sent to your DMs.\n🔗 *SeamlessRatings active — primary `{resolvedGuid}` merges {aliases.Count} alias(es): {string.Join(", ", aliases.Select(a => $"`{a}`"))}. Trace reflects combined profile.*"
                : "Elo trace has been sent to your DMs.";

            return _embedFactory.GenericEmbed(Name + " Success", description, Color.DarkBlue);
        }
    }
}
