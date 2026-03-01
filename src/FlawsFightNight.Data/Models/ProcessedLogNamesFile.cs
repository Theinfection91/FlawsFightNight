using FlawsFightNight.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Data.Models
{
    [SafeForSerialization]
    public class ProcessedLogNamesFile
    {
        public List<string> ProcessedLogFileNames { get; set; } = new();
        public List<string> IgnoredLogFileNames {  get; set; } = new();

        public ProcessedLogNamesFile() { }
    }
}
