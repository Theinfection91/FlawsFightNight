using FlawsFightNight.Core.Helpers;
using FlawsFightNight.Core.Models.Stats.UT2004;
using FluentFTP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Managers
{
    public class UT2004StatsManager : BaseDataDriven
    {
        private readonly UT2004LogParser _logParser;
        public UT2004StatsManager(DataManager dataManager, UT2004LogParser logParser) : base("UT2004StatsManager", dataManager)
        {
            _logParser = logParser;
        }

        public async Task<bool> IsLogFileProcessed(string fileName)
        {
            var processedLogs = _dataManager.GetProcessedLogNames();
            foreach (var logName in processedLogs.ProcessedLogFileNames)
            {
                if (logName == fileName)
                {
                    return true;
                }
            }
            return false;
        }

        public async Task ProcessLogFile(Stream fileStream)
        {
            var statLog = await _logParser.Parse<UT2004StatLog>(fileStream);
        }

        public async Task MarkLogFileAsProcessed(string fileName)
        {
            var processedLogs = _dataManager.GetProcessedLogNames();
            if (!processedLogs.ProcessedLogFileNames.Contains(fileName))
            {
                processedLogs.ProcessedLogFileNames.Add(fileName);
                _dataManager.SaveAndReloadProcessedLogNamesFile();
            }
        }
    }
}
