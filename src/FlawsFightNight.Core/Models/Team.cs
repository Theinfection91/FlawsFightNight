using FlawsFightNight.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Models
{
    [SafeForSerialization]
    public class Team
    {
        // Basic Info
        public string Name { get; set; }
        public int Rank { get; set; }
        public int Wins { get; set; } = 0;
        public int Losses { get; set; } = 0;
        public List<Member> Members { get; set; }

        // Streaks
        public int WinStreak { get; set; } = 0;
        public int LoseStreak { get; set; } = 0;

        // Tournament Specific Fields
        #region Ladder
        public int Rating { get; set; }
        public bool IsChallengeable { get; set; } = true;
        #endregion

        #region Round Robin
        public int TotalScore { get; set; } = 0;
        #endregion

        public Team() { }

        public string GetFormattedChallengeStatus()
        {
            if (IsChallengeable)
            {
                return "Free";
            }
            else
            {
                return "Challenged";
            }
        }

        public string GetFormattedStreakString()
        {
            if (WinStreak > 0 && LoseStreak == 0)
            {
                return $"W{WinStreak}";
            }
            if (LoseStreak > 0 && WinStreak == 0)
            {
                return $"L{LoseStreak}";
            }
            return "--";
        }

        public string GetCorrectStreakEmoji()
        {
            if (WinStreak > 0 && LoseStreak == 0)
            {
                return "📈";
            }
            if (LoseStreak > 0 && WinStreak == 0)
            {
                return "📉";
            }
            return "⌛";
        }

        public void ResetTeamToZero()
        {
            Wins = 0;
            Losses = 0;
            WinStreak = 0;
            LoseStreak = 0;
            TotalScore = 0;
        }

        public string GetMembersAsString()
        {
            return string.Join(", ", Members.Select(m => m.DisplayName));
        }

        public void RecordWin(int points = 0)
        {
            Wins++;
            WinStreak++;
            LoseStreak = 0;
            TotalScore += points;
        }

        public void RecordLoss(int points = 0)
        {
            Losses++;
            LoseStreak++;
            WinStreak = 0;
            TotalScore += points;
        }

        public bool IsTeamFull(int teamSize)
        {
            return Members.Count >= teamSize;
        }

        public bool CanAcceptAmountOfMembers(int amount, int teamSize)
        {
            return (Members.Count + amount) <= teamSize;
        }

        public bool IsMemberOnTeam(ulong discordId)
        {
            return Members.Any(m => m.DiscordId == discordId);
        }

        public bool ContainsMembers(List<Member> members, out List<Member> missingMembers)
        {
            missingMembers = members.Where(m => !IsMemberOnTeam(m.DiscordId)).ToList();
            return missingMembers.Count == 0;
        }

        public void AddMember(Member member)
        {
            Members.Add(member);
        }

        public void AddMembers(List<Member> members)
        {
            Members.AddRange(members);
        }

        public void RemoveMembers(List<Member> members)
        {
            foreach (var member in members)
            {
                Members.RemoveAll(m => m.DiscordId == member.DiscordId);
            }
        }

        public void RemoveMember(Member member)
        {
            Members.RemoveAll(m => m.DiscordId == member.DiscordId);
        }
    }
}
