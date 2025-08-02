using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Models
{
    public class Member
    {
        public ulong DiscordId { get; set; }
        public string DisplayName { get; set; }
        public Member(ulong discordId, string displayName)
        {
            DiscordId = discordId;
            DisplayName = displayName;
        }

        public override bool Equals(object? obj)
        {
            // Check if the object is a Member
            if (obj is Member otherMember)
            {
                return this.DiscordId == otherMember.DiscordId;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return DiscordId.GetHashCode(); // Use DiscordId for hash code
        }
    }
}
