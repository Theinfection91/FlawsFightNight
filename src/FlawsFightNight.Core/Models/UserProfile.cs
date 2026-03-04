using FlawsFightNight.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Models
{
    [SafeForSerialization]
    public class UserProfile
    {
        public ulong DiscordId { get; set; }
    }
}
