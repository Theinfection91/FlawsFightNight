using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Helpers.UT2004
{
    public class SeamlessRatingsAggregator
    {
        public SeamlessRatingsAggregator()
        {

        }

        public Task<bool> CanAggregateGuids(List<string> guids)
        {
            // Check if all GUIDs are the same
            var firstGuid = guids.FirstOrDefault();
            if (firstGuid == null)
                return Task.FromResult(false);
            bool allGuidsMatch = guids.All(guid => guid == firstGuid);
            return Task.FromResult(allGuidsMatch);
        }
    }
}
