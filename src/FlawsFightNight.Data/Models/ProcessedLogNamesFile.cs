using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Data.Models
{
    public class ProcessedLogNamesFile
    {
        public HashSet<Dictionary<string, string>> ProcessedLogFiles { get; set; } = new();

        public ProcessedLogNamesFile() { }
    }
}
