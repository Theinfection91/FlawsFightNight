using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Data.Handlers
{
    public class SafeSerializationBinder : ISerializationBinder
    {
        private static readonly HashSet<string> _allowedTypes = new(StringComparer.Ordinal)
        {
            // Core Models
            "FlawsFightNight.Core.Models.UserProfile",
            "FlawsFightNight.Core.Models.Team",
            "FlawsFightNight.Core.Models.Member",
            "FlawsFightNight.Core.Models.Match",
            "FlawsFightNight.Core.Models.PostMatch",
            "FlawsFightNight.Core.Models.MatchLog",
            
            // Tournament Models (polymorphic - these need TypeNameHandling)
            "FlawsFightNight.Core.Models.Tournaments.Tournament",
            "FlawsFightNight.Core.Models.Tournaments.NormalLadderTournament",
            "FlawsFightNight.Core.Models.Tournaments.DSRLadderTournament",
            "FlawsFightNight.Core.Models.Tournaments.NormalRoundRobinTournament",
            "FlawsFightNight.Core.Models.Tournaments.OpenRoundRobinTournament",

            // Match Log Models
            "FlawsFightNight.Core.Models.MatchLogs.MatchLog",
            "FlawsFightNight.Core.Models.MatchLogs.DSRLadderMatchLog",
            "FlawsFightNight.Core.Models.MatchLogs.NormalLadderMatchLog",
            "FlawsFightNight.Core.Models.MatchLogs.NormalRoundRobinMatchLog",
            "FlawsFightNight.Core.Models.MatchLogs.OpenRoundRobinMatchLog",
            
            // UT2004 Models
            "FlawsFightNight.Core.Models.UT2004.UT2004PlayerProfile",
            "FlawsFightNight.Core.Models.UT2004.UT2004StatLog",
            "FlawsFightNight.Core.Models.UT2004.UTPlayerMatchStats",
            
            // Data Models (file wrappers)
            "FlawsFightNight.Data.Models.UserProfileFile",
            "FlawsFightNight.Data.Models.TournamentDataFile",
            "FlawsFightNight.Data.Models.UT2004PlayerProfileFile",
            "FlawsFightNight.Data.Models.StatLogMatchResultsFile",
            "FlawsFightNight.Data.Models.ProcessedLogNamesFile",
            "FlawsFightNight.Data.Models.FTPCredentialFile",
            "FlawsFightNight.Data.Models.FTPCredential",
            "FlawsFightNight.Data.Models.DiscordCredentialFile",
            "FlawsFightNight.Data.Models.GitHubCredentialFile",
            "FlawsFightNight.Data.Models.PermissionsConfigFile",
            
            // System types commonly used in collections
            "System.Collections.Generic.List`1",
            "System.Collections.Generic.Dictionary`2",
            
            // Add other types as needed...
        };

        public Type BindToType(string? assemblyName, string typeName)
        {
            // Build full type name (without assembly version info)
            string fullTypeName = string.IsNullOrEmpty(assemblyName)
                ? typeName
                : $"{typeName}, {assemblyName.Split(',')[0]}";

            // Check if type is whitelisted
            if (_allowedTypes.Contains(typeName) || _allowedTypes.Contains(fullTypeName))
            {
                // Allow deserialization - resolve the type
                return Type.GetType(fullTypeName, throwOnError: false)
                       ?? Type.GetType(typeName, throwOnError: false)
                       ?? throw new InvalidOperationException($"Whitelisted type not found: {fullTypeName}");
            }

            // REJECT: Type is not whitelisted
            throw new InvalidOperationException(
                $"Type '{fullTypeName}' is not whitelisted for deserialization. " +
                $"Add it to SafeSerializationBinder._allowedTypes if it's safe.");
        }

        public void BindToName(Type serializedType, out string? assemblyName, out string? typeName)
        {
            // Use default behavior when serializing (write full type names)
            assemblyName = serializedType.Assembly.FullName;
            typeName = serializedType.FullName;
        }
    }
}
