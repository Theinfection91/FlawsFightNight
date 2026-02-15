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

        public async Task ProcessLogFile(Stream fileStream, string fileName)
        {
            var statLog = await _logParser.Parse<UT2004StatLog>(fileStream);
            if (statLog != null)
            {
                statLog.FileName = fileName;
                // Sort players by team, then by score (descending)
                statLog.Players = statLog.Players.Select(playerList =>
                    playerList.OrderBy(p => p.Team)  // Changed to OrderBy for team 0, 1, 2...
                              .ThenByDescending(p => p.Score)
                              .ToList()
                ).ToList();  // Changed from ToHashSet() to ToList()
                
                await _dataManager.SaveStatLogMatchResultFile(statLog);
                await MarkLogFileAsProcessed(fileName);
            }
        }

        public async Task MarkLogFileAsProcessed(string fileName)
        {
            var processedLogs = _dataManager.GetProcessedLogNames();
            if (!processedLogs.ProcessedLogFileNames.Contains(fileName))
            {
                processedLogs.ProcessedLogFileNames.Add(fileName);
                await _dataManager.SaveAndReloadProcessedLogNamesFile();
            }
        }
    }
}
