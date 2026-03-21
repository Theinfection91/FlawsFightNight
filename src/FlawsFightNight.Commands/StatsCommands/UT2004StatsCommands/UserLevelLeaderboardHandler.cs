using Discord;
using FlawsFightNight.Core.Models.Stats;
using FlawsFightNight.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.StatsCommands.UT2004StatsCommands
{
    public class UserLevelLeaderboardHandler : CommandHandler
    {
        private readonly EmbedFactory _embedFactory;
        private readonly UT2004StatsService _ut2004StatsService;

        public UserLevelLeaderboardHandler(EmbedFactory embedFactory, UT2004StatsService ut2004StatsService)
            : base("User Level Leaderboard")
        {
            _embedFactory = embedFactory;
            _ut2004StatsService = ut2004StatsService;
        }

        public async Task<(Embed embed, bool hasProfiles)> Handle()
        {
            var profiles = _ut2004StatsService.GetAllPrimaryPlayerProfiles();
            if (profiles == null || profiles.Count == 0)
            {
                return (
                    _embedFactory.ErrorEmbed(Name, "No UT2004 player profiles found. Stats may not have been processed yet."),
                    false);
            }

            return (_embedFactory.UT2004GeneralLeaderboardEmbed(profiles), true);
        }

        public Embed HandleSection(string section)
        {
            var profiles = _ut2004StatsService.GetAllPrimaryPlayerProfiles();
            if (profiles == null || profiles.Count == 0)
                return _embedFactory.ErrorEmbed(Name, "No UT2004 player profiles found.");

            return _embedFactory.UT2004LeaderboardEmbed(profiles, section);
        }
    }
}
