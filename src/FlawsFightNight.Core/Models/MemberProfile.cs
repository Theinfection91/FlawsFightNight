using FlawsFightNight.Core.Attributes;
using FlawsFightNight.Core.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Models
{
    [SafeForSerialization]
    public class MemberProfile
    {
        public ulong DiscordId { get; set; }
        public string DisplayName { get; set; }

        #region Tournament/Bot Related
        // Title, Level and XP
        public string Title { get; set; } = MemberLevelTitles.Novice.ToString();
        public int Level { get; set; } = 1;
        public int ExperiencePoints { get; set; } = 0;
        public int ExperienceToNextLevel => (GetNextLevelAmount(Level) - ExperiencePoints);

        // Tournament stats - Non-game specific (Not to confuse with a player's UT2004 stats)
        // These stats only pertain to the tournaments that Flaws Fight Night organizes and tracks.
        public int Wins { get; set; } = 0;
        public int Losses { get; set; } = 0;
        public int TournamentsWon { get; set; } = 0;
        public int TotalMatchesPlayed => Wins + Losses;
        public int TotalTournamentsPlayed { get; set; } = 0;
        public double WinLossRatio => (Wins + Losses) == 0 ? 0 : (double)Wins / (Wins + Losses);
        #endregion

        #region UT2004 Specific
        public List<string> RegisteredUT2004GUIDs { get; set; } = new();
        #endregion

        [JsonConstructor]
        private MemberProfile() { }

        public MemberProfile(ulong discordId, string displayName)
        {
            DiscordId = discordId;
            DisplayName = displayName;
        }

        public override bool Equals(object? obj)
        {
            if (obj is MemberProfile other)
            {
                return DiscordId == other.DiscordId;
            }
            return false;
        }
        public override int GetHashCode()
        {
            return DiscordId.GetHashCode();
        }

        public void AddExperience(int xpGained)
        {
            ExperiencePoints += xpGained;
            CheckLevelUp();
        }

        public int GetNextLevelAmount(int currentLevel)
        {
            return (int)(50 * Math.Pow(currentLevel, 1.2));
        }

        private void CheckLevelUp()
        {
            int nextLevelAmount = GetNextLevelAmount(Level);
            if (ExperiencePoints >= nextLevelAmount)
            {
                Level++;
                Title = GetTitle(Level);

                // TODO: Inform user about level up
            }
        }

        public string GetTitle(int level)
        {
            if (level >= 1 && level < 3)
                return MemberLevelTitles.Novice.ToString();
            else if (level >= 3 && level < 5)
                return MemberLevelTitles.Apprentice.ToString();
            else if (level >= 5 && level < 7)
                return MemberLevelTitles.Challenger.ToString();
            else if (level >= 7 && level < 9)
                return MemberLevelTitles.Contender.ToString();
            else if (level >= 9 && level < 11)
                return MemberLevelTitles.Elite.ToString();
            else if (level >= 11 && level < 13)
                return MemberLevelTitles.Champion.ToString();
            else if (level >= 13 && level < 15)
                return MemberLevelTitles.Master.ToString();
            else if (level >= 15 && level < 20)
                return MemberLevelTitles.Master.ToString();
            else if (level >= 20)
                return MemberLevelTitles.Legend.ToString();
            else
                return "Invalid level given";
        }
    }
}
