using FlawsFightNight.Core.Attributes;
using FlawsFightNight.Core.Models.Stats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.IO.Models
{
    [SafeForSerialization]
    public class LeaderboardChannelsFile
    {
        public List<LeaderboardChannelData> LeaderboardChannels { get; set; } = new();

        public LeaderboardChannelsFile() { }
    }
}
