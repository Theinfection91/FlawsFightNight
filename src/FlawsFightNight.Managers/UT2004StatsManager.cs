using FluentFTP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using FlawsFightNight.Core.Helpers.UT2004;
using FlawsFightNight.Core.Enums.UT2004;
using FlawsFightNight.Core.Models.UT2004;

namespace FlawsFightNight.Managers
{
    public class UT2004StatsManager : BaseDataDriven
    {
        private readonly UT2004LogParser _logParser;
        private readonly OpenSkillRatingService _ratingService;
        private readonly UTStatsDBEloRatingService _eloService;
        private int _iCTFStatLogIdCounter = 0;
        private int _TAMStatLogIdCounter = 0;
        private int _iBRStatLogIdCounter = 0;

        // Minimum matches in a given mode before its ELO peak starts being recorded.
        // Prevents the very first 1-2 matches from locking in a permanent peak.
        // Kept low for TAM/BR since many players never reach 20 matches in those modes,
        // which was causing Peak=0.0 while Rating>0 (misleading display bug).
        private const int MinCTFMatchesBeforePeak = 10;
        private const int MinTAMMatchesBeforePeak = 3;
        private const int MinBRMatchesBeforePeak = 3;

        public UT2004StatsManager(DataManager dataManager, UT2004LogParser logParser, OpenSkillRatingService ratingService) : base("UT2004StatsManager", dataManager)
        {
            _logParser = logParser;
            _ratingService = ratingService;
            _eloService = new UTStatsDBEloRatingService();
            GetStatLogCounts().Wait();
        }

        #region Stat Log Processing
        public async Task<bool> IsLogFileProcessed(string fileName)
        {
            if (_dataManager.ProcessedLogNamesFile == null)
            {
                await _dataManager.LoadProcessedLogNamesFile();
            }

            var processedLogs = _dataManager.GetProcessedLogNames();

            if (processedLogs.ProcessedLogFileNames?.Contains(fileName) == true)
                return true;

            if (processedLogs.IgnoredLogFileNames?.Contains(fileName) == true)
                return true;

            return false;
        }

        public async Task<bool> ProcessLogFile(Stream fileStream, string fileName)
        {
            var statLog = await _logParser.Parse<UT2004StatLog>(fileStream);
            if (statLog != null)
            {
                statLog.FileName = Path.ChangeExtension(fileName, ".json");
                statLog.Players = statLog.Players.Select(teamList =>
                    teamList.OrderByDescending(p => p.Score).ToList()
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

            switch (gameMode)
            {
                case UT2004GameMode.iCTF: _iCTFStatLogIdCounter++; break;
                case UT2004GameMode.TAM: _TAMStatLogIdCounter++; break;
                case UT2004GameMode.iBR: _iBRStatLogIdCounter++; break;
            }

            return $"{gameMode}{count + 1:000000}";
        }

        public async Task MarkLogFileAsProcessed(string fileName)
        {
            var processedLogs = _dataManager.GetProcessedLogNames();
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

        #region Player Profile Building with OpenSkill and ELO
        public async Task SetupPlayerProfiles()
        {
            var allMatchStats = await GetAllProcessedStatLogs();

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
                            profiles[playerStats.Guid] = new UT2004PlayerProfile(playerStats.Guid);

                        profiles[playerStats.Guid].UpdateStatsFromMatch(playerStats, match.GameMode);
                    }
                }

                // Step 2: OpenSkill ratings
                _ratingService.UpdateRatingsForMatch(match, profiles);

                // Step 3: UTStatsDB ELO ratings
                _eloService.UpdateRatingsForMatch(match, profiles);

                // Step 4: Update ELO peak for the mode just played.
                foreach (var team in match.Players)
                {
                    foreach (var playerStats in team.Where(p => !p.IsBot))
                    {
                        if (!profiles.TryGetValue(playerStats.Guid, out var profile))
                            continue;

                        switch (match.GameMode)
                        {
                            case UT2004GameMode.iCTF:
                                if (profile.TotalCTFMatches >= MinCTFMatchesBeforePeak)
                                    profile.CaptureTheFlagElo.UpdatePeak(match.MatchDate);
                                break;
                            case UT2004GameMode.TAM:
                                if (profile.TotalTAMMatches >= MinTAMMatchesBeforePeak)
                                    profile.TAMElo.UpdatePeak(match.MatchDate);
                                break;
                            case UT2004GameMode.iBR:
                                if (profile.TotalBRMatches >= MinBRMatchesBeforePeak)
                                    profile.BombingRunElo.UpdatePeak(match.MatchDate);
                                break;
                        }
                    }
                }

                // NEW: build per-player ELO changes for this match and persist a generated summary on the match
                try
                {
                    var eloChanges = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

                    foreach (var team in match.Players)
                    {
                        foreach (var playerStats in team.Where(p => !p.IsBot))
                        {
                            if (string.IsNullOrEmpty(playerStats.Guid)) continue;
                            if (!profiles.TryGetValue(playerStats.Guid, out var profile)) continue;

                            double change = match.GameMode switch
                            {
                                UT2004GameMode.iCTF => profile.CaptureTheFlagElo.Change,
                                UT2004GameMode.TAM  => profile.TAMElo.Change,
                                UT2004GameMode.iBR  => profile.BombingRunElo.Change,
                                _ => 0.0
                            };

                            if (!eloChanges.ContainsKey(playerStats.Guid))
                                eloChanges[playerStats.Guid] = change;
                        }
                    }

                    // Generate and store the summary on the stat log (RuleBasedMatchSummarizer sets match.MatchSummary too)
                    var summarizer = new RuleBasedMatchSummarizer();
                    summarizer.Summarize(match, profiles, eloChanges);

                    // Persist the updated stat log (with MatchSummary) back to disk
                    await _dataManager.SaveStatLogMatchResultFile(match);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[UT2004StatsManager] Error while generating/saving match summary for {match.FileName} on {match.MatchDate:yyyy-MM-dd}: {ex.Message}");
                }

                if (processedCount % 100 == 0)
                    Console.WriteLine($"[UT2004StatsManager] Processed {processedCount}/{chronologicalMatches.Count} matches...");
            }

            Console.WriteLine($"[UT2004StatsManager] Saving {profiles.Count} player profiles...");

            await GenerateAndPersistMatchSummaries();

            foreach (var profile in profiles.Values)
                await _dataManager.SaveUT2004PlayerProfileFile(profile);

            await _dataManager.LoadAllUT2004PlayerProfileFiles();
            //int openSkillSkipped = _ratingService.SkippedImbalancedMatches + _ratingService.SkippedInsufficientPlayers;
            //int openSkillRated = chronologicalMatches.Count - openSkillSkipped;
            //double openSkillSkipPct = (double)openSkillSkipped / chronologicalMatches.Count * 100;
            //Console.WriteLine($"\n[UT2004StatsManager] ===== RATING SUMMARY =====");
            //Console.WriteLine($"[UT2004StatsManager] Total matches processed: {chronologicalMatches.Count}");
            //Console.WriteLine($"\n[UT2004StatsManager] --- OpenSkill Ratings ---");
            //Console.WriteLine($"[UT2004StatsManager] Matches rated:              {openSkillRated}");
            //Console.WriteLine($"[UT2004StatsManager] Skipped (unequal teams):    {_ratingService.SkippedImbalancedMatches}");
            //Console.WriteLine($"[UT2004StatsManager] Skipped (not enough players): {_ratingService.SkippedInsufficientPlayers}");
            //Console.WriteLine($"[UT2004StatsManager] Total skipped:              {openSkillSkipped} ({openSkillSkipPct:F1}%)");
            //Console.WriteLine($"\n[UT2004StatsManager] --- UTStatsDB ELO Ratings ---");
            //Console.WriteLine($"[UT2004StatsManager] Matches rated:              {chronologicalMatches.Count}");
            //Console.WriteLine($"[UT2004StatsManager] Skipped (young players):    {_eloService.SkippedYoungPlayers}");
            //Console.WriteLine($"[UT2004StatsManager] Peak thresholds — CTF: {MinCTFMatchesBeforePeak}, TAM: {MinTAMMatchesBeforePeak}, BR: {MinBRMatchesBeforePeak}");
            //Console.WriteLine($"\n[UT2004StatsManager] Player profiles updated:   {profiles.Count}");
            //Console.WriteLine($"[UT2004StatsManager] ==========================\n");

            //await PrintAllPlayerRatings();
        }

        public async Task RebuildPlayerProfiles()
        {
            Console.WriteLine($"[UT2004StatsManager] Rebuilding player profiles from scratch...");

            _eloService.SkippedYoungPlayers = 0;
            _ratingService.SkippedImbalancedMatches = 0;
            _ratingService.SkippedInsufficientPlayers = 0;

            await _dataManager.DeleteUT2004ProfilesDatabase();
            await SetupPlayerProfiles();
        }

        public async Task GenerateAndPersistMatchSummaries(bool overwrite = false)
        {
            // Ensure profiles are loaded
            await _dataManager.LoadAllUT2004PlayerProfileFiles();

            var profiles = _dataManager.UT2004PlayerProfileFiles?
                .Where(f => f?.PlayerProfile != null)
                .ToDictionary(f => f.PlayerProfile.Guid, f => f.PlayerProfile, StringComparer.OrdinalIgnoreCase)
                ?? new Dictionary<string, UT2004PlayerProfile>(StringComparer.OrdinalIgnoreCase);

            var allMatches = await GetAllProcessedStatLogs();
            if (allMatches == null || allMatches.Count == 0)
            {
                Console.WriteLine("[UT2004StatsManager] No processed stat logs found.");
                return;
            }

            var summarizer = new RuleBasedMatchSummarizer();
            int updated = 0;
            int skipped = 0;

            Console.WriteLine($"[UT2004StatsManager] Generating summaries for {allMatches.Count} matches...");

            foreach (var match in allMatches.OrderBy(m => m.MatchDate))
            {
                try
                {
                    if (!overwrite && !string.IsNullOrWhiteSpace(match.MatchSummary))
                    {
                        skipped++;
                        continue;
                    }

                    // Build eloChanges from profiles (mode-specific Change fields)
                    var eloChanges = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                    foreach (var team in match.Players)
                    {
                        foreach (var p in team.Where(x => x != null && !x.IsBot))
                        {
                            if (string.IsNullOrEmpty(p.Guid)) continue;
                            if (!profiles.TryGetValue(p.Guid, out var prof)) continue;

                            double change = match.GameMode switch
                            {
                                UT2004GameMode.iCTF => prof.CaptureTheFlagElo.Change,
                                UT2004GameMode.TAM => prof.TAMElo.Change,
                                UT2004GameMode.iBR => prof.BombingRunElo.Change,
                                _ => 0.0
                            };

                            eloChanges[p.Guid] = change;
                        }
                    }

                    // Summarize and persist
                    summarizer.Summarize(match, profiles, eloChanges);
                    await _dataManager.SaveStatLogMatchResultFile(match);

                    updated++;
                    if (updated % 100 == 0)
                        Console.WriteLine($"[UT2004StatsManager] Summaries updated: {updated}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[UT2004StatsManager] Error saving summary for {match.FileName} ({match.MatchDate:yyyy-MM-dd}): {ex.Message}");
                }
            }

            Console.WriteLine($"[UT2004StatsManager] Done. Updated: {updated}, Skipped (already present): {skipped}");
        }

        public async Task PrintAllPlayerRatings()
        {
            // Make temp list and sort by most total matches across all modes
            List<UT2004PlayerProfile> tempList = new List<UT2004PlayerProfile>(_dataManager.UT2004PlayerProfileFiles.Select(f => f.PlayerProfile));
            tempList = tempList.OrderByDescending(p => p.TotalMatches).ToList();
            foreach (var profile in tempList)
            {
                Console.WriteLine($"Player: {profile.Guid}");
                Console.WriteLine($"  CTF ELO: {profile.CaptureTheFlagElo.Rating:F1} (Change: {profile.CaptureTheFlagElo.Change:+0.0;-0.0}, Peak: {profile.CaptureTheFlagElo.Peak:F1} on {profile.CaptureTheFlagElo.PeakDate:yyyy-MM-dd})");
                Console.WriteLine($"  TAM ELO: {profile.TAMElo.Rating:F1} (Change: {profile.TAMElo.Change:+0.0;-0.0}, Peak: {profile.TAMElo.Peak:F1} on {profile.TAMElo.PeakDate:yyyy-MM-dd})");
                Console.WriteLine($"  BR ELO: {profile.BombingRunElo.Rating:F1} (Change: {profile.BombingRunElo.Change:+0.0;-0.0}, Peak: {profile.BombingRunElo.Peak:F1} on {profile.BombingRunElo.PeakDate:yyyy-MM-dd})");
                Console.WriteLine($"  Total Matches - CTF: {profile.TotalCTFMatches}, TAM: {profile.TotalTAMMatches}, BR: {profile.TotalBRMatches}");
                Console.WriteLine();
            }
        }
        #endregion
    }
}
