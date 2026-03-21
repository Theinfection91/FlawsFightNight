using FlawsFightNight.Core.Attributes;
using FlawsFightNight.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Models.Stats
{
    [SafeForSerialization]
    public class LeaderboardChannelData
    {
        public ulong ChannelId { get; set; }
        public LeaderboardChannelTypes LeaderboardChannelType { get; set; }

        public LeaderboardChannelData() { }
    }
}
