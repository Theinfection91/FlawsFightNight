using FlawsFightNight.Core.Interfaces;
using FlawsFightNight.Core.Models.Stats.UT2004;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Helpers
{
    public class UT2004LogParser : ILogParser
    {
        public async Task<T?> Parse<T>(Stream fileStream)
        {
            if (typeof(T) == typeof(UT2004StatLog))
            {
                return (T?)(object)await ParseUT2004StatLog(fileStream);
            }

            return default;
        }

        private async Task<UT2004StatLog> ParseUT2004StatLog(Stream fileStream)
        {
            using (var reader = new StreamReader(fileStream))
            {
                var players = new Dictionary<int, UTPlayerStats>();

                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    var parts = line.Split('\t');
                    if (parts.Length > 2) continue;

                    string type = parts[1];
                    switch (type)
                    {
                        case "C":
                            return null;// Connection: 0.89 C [ID] [GUID] [Name]
                    }
                }
                // Implementation for parsing UT2004StatLog
                return new UT2004StatLog();
            }
        }
    }
}
