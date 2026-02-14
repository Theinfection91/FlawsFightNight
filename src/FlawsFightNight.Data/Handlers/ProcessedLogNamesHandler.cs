using FlawsFightNight.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Data.Handlers
{
    public class ProcessedLogNamesHandler : BaseDataHandler<ProcessedLogNamesFile>
    {
        public ProcessedLogNamesHandler() : base("processed_log_names.json", "Databases")
        {
        }
    }
}
