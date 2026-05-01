using Discord;
using FlawsFightNight.Services;

namespace FlawsFightNight.Commands.SettingsCommands.UT2004AdminCommands
{
    public class GetPlayerProfileByGuidHandler : CommandHandler
    {
        private readonly EmbedFactory _embedFactory;
        private readonly UT2004StatsService _ut2004StatsService;

        public GetPlayerProfileByGuidHandler(EmbedFactory embedFactory, UT2004StatsService ut2004StatsService) : base("Get Player Profile By GUID")
        {
            _embedFactory = embedFactory;
            _ut2004StatsService = ut2004StatsService;
        }

        public Embed GetPlayerProfileByGuidProcess(string guid)
        {
            if (string.IsNullOrWhiteSpace(guid))
                return _embedFactory.ErrorEmbed(Name, "Please provide a non-empty GUID.");

            var profile = _ut2004StatsService.GetPlayerProfileByGuid(guid);
            if (profile == null)
                return _embedFactory.ErrorEmbed(Name, $"No player profile found for GUID: `{guid}`.");

            return _embedFactory.UT2004ProfileGeneralEmbed(profile);
        }
    }
}