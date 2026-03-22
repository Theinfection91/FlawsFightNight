using FlawsFightNight.Core.Attributes;
using FlawsFightNight.Core.Models.Stats;
using System.Collections.Generic;

namespace FlawsFightNight.IO.Models
{
    [SafeForSerialization]
    public class LiveViewChannelsFile
    {
        public List<LeaderboardChannelData> LeaderboardChannels { get; set; } = new();
        
        // Single ID allowed for the admin feed
        public ulong AdminChannelFeedId { get; set; } = 0;

        public LiveViewChannelsFile() { }
    }
}