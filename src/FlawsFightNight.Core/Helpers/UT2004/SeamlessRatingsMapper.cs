using FlawsFightNight.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Helpers.UT2004
{
    public class SeamlessRatingsMapper
    {
        // UT2004 GUID → primary (oldest) GUID
        private Dictionary<string, string> _guidToPrimary = new(StringComparer.OrdinalIgnoreCase);

        public void BuildAliasMap(List<MemberProfile> profiles)
        {
            _guidToPrimary.Clear();

            foreach (var profile in profiles)
            {
                if (profile.RegisteredUT2004GUIDs == null || profile.RegisteredUT2004GUIDs.Count < 2)
                    continue;

                // [0] is the oldest GUID — that's the primary identity
                string primaryGuid = profile.RegisteredUT2004GUIDs[0];

                foreach (var guid in profile.RegisteredUT2004GUIDs)
                {
                    _guidToPrimary[guid] = primaryGuid;
                }
            }
        }

        public string Resolve(string guid)
        {
            return _guidToPrimary.TryGetValue(guid, out var primary) ? primary : guid;
        }

        /// <summary>
        /// Returns all non-primary (alias) GUIDs that resolve to the given primary GUID.
        /// Returns empty if the GUID has no aliases or is not a primary.
        /// </summary>
        public IEnumerable<string> GetAliasesForPrimary(string primaryGuid)
            => _guidToPrimary
                .Where(kv => kv.Value.Equals(primaryGuid, StringComparison.OrdinalIgnoreCase)
                          && !kv.Key.Equals(primaryGuid, StringComparison.OrdinalIgnoreCase))
                .Select(kv => kv.Key);

        /// <summary>
        /// Returns all non-primary (alias) GUIDs — the ones that resolve to something else.
        /// </summary>
        public IEnumerable<string> GetAliasGuids()
            => _guidToPrimary.Where(kv => !kv.Key.Equals(kv.Value, StringComparison.OrdinalIgnoreCase))
                             .Select(kv => kv.Key);

        public bool IsAlias(string guid)
            => _guidToPrimary.TryGetValue(guid, out var primary)
               && !guid.Equals(primary, StringComparison.OrdinalIgnoreCase);

        public bool HasAliases => _guidToPrimary.Count > 0;
    }
}
