using FlawsFightNight.Core.Enums.UT2004;
using FlawsFightNight.Core.Models.UT2004;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Helpers.UT2004
{
    public static class UT2004PlayerProfileBuilder
    {
        public async static Task<UT2004PlayerProfile> BuildProfileFromMatchStats(UTPlayerMatchStats playerMatchStats, UT2004GameMode gameMode, DateTime matchDate, UT2004PlayerProfile? existingProfile = null)
        {
            var profile = existingProfile ?? new UT2004PlayerProfile(playerMatchStats.Guid);

            // Use the profile's built-in method to update all stats and record match timestamps
            profile.UpdateStatsFromMatch(playerMatchStats, gameMode, matchDate);

            return profile;
        }

        public async static Task<bool> IsGuidInDatabase(string guid, List<UT2004PlayerProfile> allPlayerProfiles)
        {
            return allPlayerProfiles.Any(profile => profile.Guid == guid);
        }

        public static async Task<List<UT2004PlayerProfile>> InitializeFreshDatabase(List<UT2004StatLog> allMatchStats)
        {
            var profiles = new Dictionary<string, UT2004PlayerProfile>();

            foreach (var match in allMatchStats)
            {
                foreach (var team in match.Players)
                {
                    foreach (var playerStats in team)
                    {
                        if (string.IsNullOrEmpty(playerStats.Guid))
                            continue;

                        if (!profiles.ContainsKey(playerStats.Guid))
                        {
                            profiles[playerStats.Guid] = await BuildProfileFromMatchStats(playerStats, match.GameMode, match.MatchDate);
                        }
                        else
                        {
                            profiles[playerStats.Guid] = await BuildProfileFromMatchStats(playerStats, match.GameMode, match.MatchDate, profiles[playerStats.Guid]);
                        }
                    }
                }
            }

            return profiles.Values.ToList();
        }
    }
}
