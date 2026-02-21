using FlawsFightNight.Core.Models.Stats.UT2004;
using FluentFTP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using FlawsFightNight.Core.Helpers.UT2004;
using FlawsFightNight.Core.Enums.UT2004;

namespace FlawsFightNight.Managers
{
    public class UT2004StatsManager : BaseDataDriven
    {
        private readonly UT2004LogParser _logParser;
        private readonly OpenSkillRatingService _ratingService;
        private int _iCTFStatLogIdCounter = 0;
        private int _TAMStatLogIdCounter = 0;
        private int _iBRStatLogIdCounter = 0;

        public UT2004StatsManager(DataManager dataManager, UT2004LogParser logParser, OpenSkillRatingService ratingService) : base("UT2004StatsManager", dataManager)
        {
            _logParser = logParser;
            _ratingService = ratingService;
            GetStatLogCounts().Wait();
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
                // Players are already grouped by team — just sort within each team
                statLog.Players = statLog.Players.Select(teamList =>
                    teamList.OrderByDescending(p => p.Score)
                            .ToList()
                ).ToList();
                statLog.Id = await GenerateStatLogId(statLog.GameMode);
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

        public async Task GetStatLogCounts()
        {
            _iCTFStatLogIdCounter = await _dataManager.GetStatLogCount(UT2004GameMode.iCTF);
            _TAMStatLogIdCounter = await _dataManager.GetStatLogCount(UT2004GameMode.TAM);
            _iBRStatLogIdCounter = await _dataManager.GetStatLogCount(UT2004GameMode.iBR);
        }

        public async Task<string> GenerateStatLogId(UT2004GameMode gameMode)
        {
            int count = gameMode switch
            {
                UT2004GameMode.iCTF => _iCTFStatLogIdCounter,
                UT2004GameMode.TAM => _TAMStatLogIdCounter,
                UT2004GameMode.iBR => _iBRStatLogIdCounter,
                _ => 0
            };

            // Increment the appropriate counter
            switch (gameMode)
            {
                case UT2004GameMode.iCTF:
                    _iCTFStatLogIdCounter++;
                    break;
                case UT2004GameMode.TAM:
                    _TAMStatLogIdCounter++;
                    break;
                case UT2004GameMode.iBR:
                    _iBRStatLogIdCounter++;
                    break;
            }

            return $"{gameMode}{count + 1:000000}";

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
                        profiles[playerStats.Guid].UpdateStatsFromMatch(playerStats, match.GameMode);
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

            // Calculate statistics
            int totalSkipped = _ratingService.SkippedImbalancedMatches + _ratingService.SkippedInsufficientPlayers;
            int ratedMatches = chronologicalMatches.Count - totalSkipped;
            double skipPercentage = (double)totalSkipped / chronologicalMatches.Count * 100;

            Console.WriteLine($"\n[UT2004StatsManager] ===== RATING SUMMARY =====");
            Console.WriteLine($"[UT2004StatsManager] Total matches processed: {chronologicalMatches.Count}");
            Console.WriteLine($"[UT2004StatsManager] Matches rated: {ratedMatches}");
            Console.WriteLine($"[UT2004StatsManager] Skipped (unequal team sizes): {_ratingService.SkippedImbalancedMatches}");
            Console.WriteLine($"[UT2004StatsManager] Skipped (insufficient players): {_ratingService.SkippedInsufficientPlayers}");
            Console.WriteLine($"[UT2004StatsManager] Total skipped: {totalSkipped} ({skipPercentage:F1}%)");
            Console.WriteLine($"[UT2004StatsManager] Player profiles updated: {profiles.Count}");
            Console.WriteLine($"[UT2004StatsManager] ==========================\n");
        }

        public async Task RebuildPlayerProfiles()
        {
            Console.WriteLine($"[UT2004StatsManager] Rebuilding player profiles from scratch...");
            await _dataManager.DeleteUT2004ProfilesDatabase();
            await SetupPlayerProfiles();
        #endregion

            //public async Task<string> PredictMatchOutcome()
            //{
            //    var teamA = new List<UT2004PlayerProfile>();
            //    var teamB = new List<UT2004PlayerProfile>();
            //    //teamA.Add(await _dataManager.GetUT2004PlayerProfile("f65f3f7e0496815de17a4713604e5016")); // Serge
            //    teamA.Add(await _dataManager.GetUT2004PlayerProfile("cc64eb45e190de68c0deaf75231e1ab8")); // Relapse
            //    teamB.Add(await _dataManager.GetUT2004PlayerProfile("f7fc75c7f7f3cf3cfc9b700b73925586")); // BloodBath

            //    var winProbability = _ratingService.PredictWin(teamA, teamB);
            //    return $"Team A win probability: {winProbability:P2}, Team B win probability: {1 - winProbability:P2}";
            //}
        }
    }
}
