using FlawsFightNight.Core.Attributes;
using FlawsFightNight.Core.Models.UT2004;
using System.Collections.Generic;

namespace FlawsFightNight.IO.Models
{
    [SafeForSerialization]
    public class AdminIgnoredLogsFile
    {
        public List<AdminIgnoredLogEntry> Entries { get; set; } = new();

        public AdminIgnoredLogsFile() { }
    }
}
