using FlawsFightNight.Core.Models.Stats.UT2004;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Helpers.UT2004
{
    public static class UT2004PlayerProfileBuilder
    {
        public async static Task<UT2004PlayerProfile> BuildProfileFromMatchStats(UTPlayerMatchStats playerMatchStats, UT2004PlayerProfile? existingProfile = null)
        {
            var profile = existingProfile ?? new UT2004PlayerProfile(playerMatchStats.Guid);
            // Update identity
            if (profile.CurrentName != playerMatchStats.LastKnownName)
            {
                if (!profile.PreviousNames.Contains(profile.CurrentName))
                {
                    profile.PreviousNames.Add(profile.CurrentName);
                }
                profile.CurrentName = playerMatchStats.LastKnownName;
            }
            // Update match history
            profile.TotalMatches += 1;
            if (playerMatchStats.IsWinner) profile.Wins += 1; else profile.Losses += 1;
            profile.LastPlayed = DateTime.UtcNow;

            // Update skill rating (placeholder - implement OpenSkill update here)
            // (In a real implementation, you'd need the opponent's profiles and the match outcome to update Mu and Sigma properly)

            // Update cumulative stats
            profile.TotalKills += playerMatchStats.Kills;
            profile.TotalDeaths += playerMatchStats.Deaths;
            profile.TotalSuicides += playerMatchStats.Suicides;
            profile.TotalHeadshots += playerMatchStats.Headshots;
            profile.TotalFlagCaptures += playerMatchStats.FlagCaptures;
            profile.TotalFlagReturns += playerMatchStats.FlagReturns;
            profile.TotalScore += playerMatchStats.Score;
            // Update career bests
            if (playerMatchStats.BestKillStreak > profile.BestKillStreak) profile.BestKillStreak = playerMatchStats.BestKillStreak;
            if (playerMatchStats.BestMultiKill > profile.BestMultiKill) profile.BestMultiKill = playerMatchStats.BestMultiKill;
            if (playerMatchStats.Kills > profile.MostKillsInMatch) profile.MostKillsInMatch = playerMatchStats.Kills;
            if (playerMatchStats.FlagCaptures > profile.MostFlagCapsInMatch) profile.MostFlagCapsInMatch = playerMatchStats.FlagCaptures;
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
                        if (!profiles.ContainsKey(playerStats.Guid))
                        {
                            profiles[playerStats.Guid] = await BuildProfileFromMatchStats(playerStats);
                        }
                        else
                        {
                            profiles[playerStats.Guid] = await BuildProfileFromMatchStats(playerStats, profiles[playerStats.Guid]);
                        }
                    }
                }
            }
            return profiles.Values.ToList();
        }
    }
}
