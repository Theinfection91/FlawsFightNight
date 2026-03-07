using FlawsFightNight.Core.Attributes;
using System;

namespace FlawsFightNight.Core.Models.UT2004
{
    [SafeForSerialization]
    public class AdminIgnoredLogEntry
    {
        public string StatLogId { get; set; }
        public ulong AdminDiscordID { get; set; }
        public string AdminName { get; set; }
        public DateTime IgnoredAt { get; set; }

        public AdminIgnoredLogEntry() { }
    }
}