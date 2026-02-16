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
        private readonly OpenSkillRatingService _ratingService;

        public UT2004StatsManager(DataManager dataManager, UT2004LogParser logParser, OpenSkillRatingService ratingService) : base("UT2004StatsManager", dataManager)
        {
            _logParser = logParser;
            _ratingService = ratingService;
        }

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

        #region Player Profile Building with OpenSkill
        public async Task SetupPlayerProfiles()
        {
            var allMatchStats = await GetAllProcessedStatLogs();
            
            // Sort matches chronologically using timestamps from log files
            var chronologicalMatches = allMatchStats
                .OrderBy(m => m.MatchDate)
                .ToList();
            
            Console.WriteLine($"[UT2004StatsManager] Processing {chronologicalMatches.Count} matches chronologically...");
            Console.WriteLine($"[UT2004StatsManager] Date range: {chronologicalMatches.First().MatchDate:yyyy-MM-dd} to {chronologicalMatches.Last().MatchDate:yyyy-MM-dd}");
            
            var profiles = new Dictionary<string, UT2004PlayerProfile>();
            
            int processedCount = 0;
            foreach (var match in chronologicalMatches)
            {
                processedCount++;
                
                // Step 1: Update cumulative stats
                foreach (var team in match.Players)
                {
                    foreach (var playerStats in team.Where(p => !p.IsBot))
                    {
                        if (!profiles.ContainsKey(playerStats.Guid))
                        {
                            profiles[playerStats.Guid] = new UT2004PlayerProfile(playerStats.Guid);
                        }
                        profiles[playerStats.Guid].UpdateStatsFromMatch(playerStats);
                    }
                }
                
                // Step 2: Calculate OpenSkill ratings for this match
                _ratingService.UpdateRatingsForMatch(match, profiles);
                
                // Progress indicator every 100 matches
                if (processedCount % 100 == 0)
                {
                    Console.WriteLine($"[UT2004StatsManager] Processed {processedCount}/{chronologicalMatches.Count} matches...");
                }
            }
            
            // Save all profiles
            Console.WriteLine($"[UT2004StatsManager] Saving {profiles.Count} player profiles...");
            foreach (var profile in profiles.Values)
            {
                await _dataManager.SaveUT2004PlayerProfileFile(profile);
            }
            
            Console.WriteLine($"[UT2004StatsManager] Complete... Updated {profiles.Count} player profiles across {chronologicalMatches.Count} matches");
        }
        #endregion
    }
}
