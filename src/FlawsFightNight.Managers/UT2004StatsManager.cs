using FlawsFightNight.Core.Models.Stats.UT2004;
using FluentFTP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using FlawsFightNight.Core.Helpers.UT2004;

namespace FlawsFightNight.Managers
{
    public class UT2004StatsManager : BaseDataDriven
    {
        private readonly UT2004LogParser _logParser;
        public UT2004StatsManager(DataManager dataManager, UT2004LogParser logParser) : base("UT2004StatsManager", dataManager)
        {
            _logParser = logParser;
        }

        #region Player Profile Building
        public async Task SetupPlayerProfiles()
        {
            var allMatchStats = await GetAllProcessedStatLogs();
            var allPlayerProfiles = await UT2004PlayerProfileBuilder.InitializeFreshDatabase(allMatchStats);
            foreach (var profile in allPlayerProfiles )
            {
                await _dataManager.SaveUT2004PlayerProfileFile(profile);
            }
        }
        #endregion

        #region Stat Log Processing
        public async Task<bool> IsLogFileProcessed(string fileName)
        {
            // Ensure the processed log names file is loaded
            if (_dataManager.ProcessedLogNamesFile == null)
            {
                await _dataManager.LoadProcessedLogNamesFile();
            }

            var processedLogs = _dataManager.GetProcessedLogNames();
            
            // Check if in processed list
            if (processedLogs.ProcessedLogFileNames?.Contains(fileName) == true)
            {
                return true;
            }
            
            // Check if in ignored list
            if (processedLogs.IgnoredLogFileNames?.Contains(fileName) == true)
            {
                return true;
            }
            
            return false;
        }

        public async Task<bool> ProcessLogFile(Stream fileStream, string fileName)
        {
            var statLog = await _logParser.Parse<UT2004StatLog>(fileStream);
            if (statLog != null)
            {
                statLog.FileName = Path.ChangeExtension(fileName, ".json");

                statLog.Players = statLog.Players.Select(playerList =>
                    playerList.OrderBy(p => p.Team)
                              .ThenByDescending(p => p.Score)
                              .ToList()
                ).ToList();

                await _dataManager.SaveStatLogMatchResultFile(statLog);
                await MarkLogFileAsProcessed(fileName);
                return true;
            }
            else
            {
                await MarkLogFileAsIgnored(fileName);
                return false;
            }
        }

        public async Task MarkLogFileAsProcessed(string fileName)
        {
            var processedLogs = _dataManager.GetProcessedLogNames();
            
            // Ensure lists are initialized
            processedLogs.ProcessedLogFileNames ??= new List<string>();
            
            if (!processedLogs.ProcessedLogFileNames.Contains(fileName))
            {
                processedLogs.ProcessedLogFileNames.Add(fileName);
                await _dataManager.SaveAndReloadProcessedLogNamesFile();
            }
        }

        public async Task MarkLogFileAsIgnored(string fileName)
        {
            var processedLogs = _dataManager.GetProcessedLogNames();
            
            // Ensure lists are initialized
            processedLogs.IgnoredLogFileNames ??= new List<string>();
            
            if (!processedLogs.IgnoredLogFileNames.Contains(fileName))
            {
                processedLogs.IgnoredLogFileNames.Add(fileName);
                await _dataManager.SaveAndReloadProcessedLogNamesFile();
            }
        }

        public async Task<List<UT2004StatLog>> GetAllProcessedStatLogs()
        {
            var statLogFiles = await _dataManager.LoadAllStatLogMatchResultFiles();
            return statLogFiles.Select(file => file.StatLog).ToList();
        }
        #endregion
    }
}
