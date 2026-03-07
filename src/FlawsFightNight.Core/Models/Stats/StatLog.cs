using FlawsFightNight.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Models.Stats
{
    [SafeForSerialization]
    public abstract class StatLog
    {
        public string ServerName { get; set; }
        public string IPAddress { get; set; }
        public bool IsAllowedByAdmin { get; set; } = true;
        public StatLog() { }
    }
}
