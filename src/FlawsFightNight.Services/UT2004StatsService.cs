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
using Discord.WebSocket;

namespace FlawsFightNight.Services
{
    public class UT2004StatsService : BaseDataDriven
    {
        private readonly DiscordSocketClient _client;

        private readonly UT2004LogParser _logParser;
        private readonly OpenSkillRatingService _ratingService;
        private readonly UTStatsDBEloRatingService _eloService;
        private readonly SeamlessRatingsMapper _ratingsMapper;
        private int _iCTFStatLogIdCounter = 0;
        private int _TAMStatLogIdCounter = 0;
        private int _iBRStatLogIdCounter = 0;

        private const int MinCTFMatchesBeforePeak = 10;
        private const int MinTAMMatchesBeforePeak = 3;
        private const int MinBRMatchesBeforePeak = 3;

        public UT2004StatsService(DataContext dataContext, DiscordSocketClient client, UT2004LogParser logParser, OpenSkillRatingService ratingService, UTStatsDBEloRatingService eloService, SeamlessRatingsMapper ratingsMapper) : base("UT2004StatsService", dataContext)
        {
            _client = client;
            _logParser = logParser;
            _ratingService = ratingService;
            _ratingsMapper = ratingsMapper;
            _eloService = eloService;
        }

        public async Task InitializeAsync()
        {
            await GetStatLogCounts();
        }

        #region Stat Log Processing
        public async Task<bool> IsLogFileProcessed(string fileName)
        {
            if (_dataContext.ProcessedLogNamesFile == null)
            {
                await _dataContext.LoadProcessedLogNamesFile();
            }

            var processedLogs = _dataContext.GetProcessedLogNames();

            if (processedLogs.ProcessedLogFileNames?.Contains(fileName) == true)
                return true;

            if (processedLogs.IgnoredLogFileNames?.Contains(fileName) == true)
                return true;

            return false;
        }

        public async Task<bool> ProcessLogFile(Stream fileStream, string fileName, string serverName, string serverAddress)
        {
            var statLog = await _logParser.Parse<UT2004StatLog>(fileStream);
            if (statLog != null)
            {
                statLog.FileName = Path.ChangeExtension(fileName, ".json");
                statLog.ServerName = serverName;
                statLog.IPAddress = serverAddress;
                statLog.Players = statLog.Players.Select(teamList =>
                    teamList.OrderByDescending(p => p.Score).ToList()
                ).ToList();
                statLog.Id = await GenerateStatLogId(statLog.GameMode);
                await _dataContext.SaveStatLogMatchResultFile(statLog);
                await _dataContext.AddStatLogIndexEntry(new StatLogIndexEntry
                {
                    Id = statLog.Id,
                    MatchDate = statLog.MatchDate,
                    ServerName = statLog.ServerName
                });
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
            _iCTFStatLogIdCounter = await _dataContext.GetStatLogCount(UT2004GameMode.iCTF);
            _TAMStatLogIdCounter = await _dataContext.GetStatLogCount(UT2004GameMode.TAM);
            _iBRStatLogIdCounter = await _dataContext.GetStatLogCount(UT2004GameMode.iBR);
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
            var processedLogs = _dataContext.GetProcessedLogNames();
            processedLogs.ProcessedLogFileNames ??= new List<string>();

            if (!processedLogs.ProcessedLogFileNames.Contains(fileName))
            {
                processedLogs.ProcessedLogFileNames.Add(fileName);
                await _dataContext.SaveAndReloadProcessedLogNamesFile();
            }
        }

        public async Task MarkLogFileAsIgnored(string fileName)
        {
            var processedLogs = _dataContext.GetProcessedLogNames();
            processedLogs.IgnoredLogFileNames ??= new List<string>();

            if (!processedLogs.IgnoredLogFileNames.Contains(fileName))
            {
                processedLogs.IgnoredLogFileNames.Add(fileName);
                await _dataContext.SaveAndReloadProcessedLogNamesFile();
            }
        }

        public async Task<List<UT2004StatLog>> GetAllProcessedStatLogs()
        {
            var statLogFiles = await _dataContext.LoadAllStatLogMatchResultFiles();
            return statLogFiles.Select(file => file.StatLog).ToList();
        }

        #endregion

        #region Player Profile Building with OpenSkill and ELO
        public async Task SetupPlayerProfiles()
        {
            // Prime the SeamlessRatings alias map
            var memberProfiles = _dataContext.MemberProfileFiles
                .Select(f => f.MemberProfile)
                .ToList();
            _ratingsMapper.BuildAliasMap(memberProfiles);

            if (_ratingsMapper.HasAliases)
            {
                Console.WriteLine($"[SeamlessRatings] Active aliases detected — merged GUIDs will be treated as one identity.");
                foreach (var profile in memberProfiles.Where(p => p.RegisteredUT2004GUIDs?.Count >= 2))
                {
                    string primary = profile.RegisteredUT2004GUIDs[0];
                    Console.WriteLine($"[SeamlessRatings] Player: {profile.DisplayName} | Primary GUID: {primary}");
                    foreach (var guid in profile.RegisteredUT2004GUIDs.Skip(1))
                        Console.WriteLine($"[SeamlessRatings]   Alias: {guid} → {primary}");
                }
            }
            else
            {
                Console.WriteLine($"[SeamlessRatings] No aliases active — all GUIDs treated independently.");
            }

            // Ensure the admin ignored logs file is loaded before filtering
            if (_dataContext.AdminIgnoredLogsFile == null)
                await _dataContext.LoadAdminIgnoredLogsFile();

            var allMatchStats = await GetAllProcessedStatLogs();

            var chronologicalMatches = allMatchStats
                .Where(m => !_dataContext.IsStatLogIgnored(m.Id))
                .OrderBy(m => m.MatchDate)
                .ToList();

            int ignoredCount = allMatchStats.Count - chronologicalMatches.Count;
            if (ignoredCount > 0)
                Console.WriteLine($"[UT2004StatsService] Skipping {ignoredCount} admin-ignored stat log(s) from profile calculations.");

            Console.WriteLine($"[UT2004StatsService] Processing {chronologicalMatches.Count} matches chronologically...");
            Console.WriteLine($"[UT2004StatsService] Date range: {chronologicalMatches.First().MatchDate:yyyy-MM-dd} to {chronologicalMatches.Last().MatchDate:yyyy-MM-dd}");

            var profiles = new Dictionary<string, UT2004PlayerProfile>();

            // Standalone per-raw-GUID profiles for aliased players.
            // Stats only — no ELO/OpenSkill (those live on the merged primary profile).
            // Preserved so players can switch primary without losing their history.
            var rawProfiles = new Dictionary<string, UT2004PlayerProfile>(StringComparer.OrdinalIgnoreCase);

            var mergeLog = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            int processedCount = 0;
            foreach (var match in chronologicalMatches)
            {
                processedCount++;

                // Step 1: Update cumulative stats
                foreach (var team in match.Players)
                {
                    foreach (var playerStats in team.Where(p => !p.IsBot))
                    {
                        var resolvedGuid = _ratingsMapper.Resolve(playerStats.Guid);

                        // DEBUG: Log the first time we see a GUID get merged
                        if (resolvedGuid != playerStats.Guid && !mergeLog.ContainsKey(playerStats.Guid))
                        {
                            mergeLog[playerStats.Guid] = resolvedGuid;
                            Console.WriteLine($"[SeamlessRatings] First merge hit in replay — raw: {playerStats.Guid} → primary: {resolvedGuid} (match: {match.FileName}, date: {match.MatchDate:yyyy-MM-dd})");
                        }

                        // Merged profile (primary GUID)
                        if (!profiles.ContainsKey(resolvedGuid))
                            profiles[resolvedGuid] = new UT2004PlayerProfile(resolvedGuid);
                        profiles[resolvedGuid].UpdateStatsFromMatch(playerStats, match.GameMode, match.MatchDate);

                        // Standalone raw profile — only for alias GUIDs, stats only, no ratings
                        if (_ratingsMapper.IsAlias(playerStats.Guid))
                        {
                            if (!rawProfiles.ContainsKey(playerStats.Guid))
                                rawProfiles[playerStats.Guid] = new UT2004PlayerProfile(playerStats.Guid);
                            rawProfiles[playerStats.Guid].UpdateStatsFromMatch(playerStats, match.GameMode, match.MatchDate);
                        }
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
                        var resolvedGuid = _ratingsMapper.Resolve(playerStats.Guid);
                        if (!profiles.TryGetValue(resolvedGuid, out var profile))
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

                try
                {
                    var eloChanges = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

                    foreach (var team in match.Players)
                    {
                        foreach (var playerStats in team.Where(p => !p.IsBot))
                        {
                            if (string.IsNullOrEmpty(playerStats.Guid)) continue;
                            var resolvedGuid = _ratingsMapper.Resolve(playerStats.Guid);
                            if (!profiles.TryGetValue(resolvedGuid, out var profile)) continue;

                            double change = match.GameMode switch
                            {
                                UT2004GameMode.iCTF => profile.CaptureTheFlagElo.Change,
                                UT2004GameMode.TAM => profile.TAMElo.Change,
                                UT2004GameMode.iBR => profile.BombingRunElo.Change,
                                _ => 0.0
                            };

                            if (!eloChanges.ContainsKey(resolvedGuid))
                                eloChanges[resolvedGuid] = change;
                        }
                    }

                    var summarizer = new RuleBasedMatchSummarizer();
                    summarizer.Summarize(match, profiles, eloChanges);

                    await _dataContext.SaveStatLogMatchResultFile(match);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[UT2004StatsService] Error while generating/saving match summary for {match.FileName} on {match.MatchDate:yyyy-MM-dd}: {ex.Message}");
                }

                if (processedCount % 100 == 0)
                    Console.WriteLine($"[UT2004StatsService] Processed {processedCount}/{chronologicalMatches.Count} matches...");
            }

            if (mergeLog.Count > 0)
            {
                Console.WriteLine($"\n[SeamlessRatings] ===== MERGE SUMMARY =====");
                Console.WriteLine($"[SeamlessRatings] {mergeLog.Count} alias GUID(s) were merged during this replay.");
                var affectedPrimaries = mergeLog.Values.Distinct().ToList();
                foreach (var primaryGuid in affectedPrimaries)
                {
                    if (!profiles.TryGetValue(primaryGuid, out var merged)) continue;
                    Console.WriteLine($"[SeamlessRatings] Merged Profile → GUID: {primaryGuid} | Name: {merged.CurrentName}");
                    Console.WriteLine($"  Total Matches: CTF={merged.TotalCTFMatches} TAM={merged.TotalTAMMatches} BR={merged.TotalBRMatches}");
                    Console.WriteLine($"  CTF ELO: {merged.CaptureTheFlagElo.Rating:F1} | TAM ELO: {merged.TAMElo.Rating:F1} | BR ELO: {merged.BombingRunElo.Rating:F1}");
                }
                Console.WriteLine($"[SeamlessRatings] ==========================\n");
            }

            Console.WriteLine($"[UT2004StatsService] Saving {profiles.Count} player profiles...");

            await GenerateAndPersistMatchSummaries();

            foreach (var profile in profiles.Values)
                await _dataContext.SaveUT2004PlayerProfileFile(profile);

            // Save standalone raw profiles for aliased GUIDs
            if (rawProfiles.Count > 0)
            {
                Console.WriteLine($"[SeamlessRatings] Saving {rawProfiles.Count} standalone raw GUID profile(s)...");
                foreach (var rawProfile in rawProfiles.Values)
                    await _dataContext.SaveUT2004PlayerProfileFile(rawProfile);
            }

            await _dataContext.LoadAllUT2004PlayerProfileFiles();
        }

        public async Task RebuildPlayerProfiles()
        {
            Console.WriteLine($"[UT2004StatsService] Rebuilding player profiles from scratch...");

            _eloService.SkippedYoungPlayers = 0;
            _ratingService.SkippedImbalancedMatches = 0;
            _ratingService.SkippedInsufficientPlayers = 0;

            await _dataContext.DeleteUT2004ProfilesDatabase();
            await SetupPlayerProfiles();
        }

        public async Task GenerateAndPersistMatchSummaries(bool overwrite = false)
        {
            // Ensure profiles are loaded
            await _dataContext.LoadAllUT2004PlayerProfileFiles();

            var profiles = _dataContext.UT2004PlayerProfileFiles?
                .Where(f => f?.PlayerProfile != null)
                .ToDictionary(f => f.PlayerProfile.Guid, f => f.PlayerProfile, StringComparer.OrdinalIgnoreCase)
                ?? new Dictionary<string, UT2004PlayerProfile>(StringComparer.OrdinalIgnoreCase);

            var allMatches = await GetAllProcessedStatLogs();
            if (allMatches == null || allMatches.Count == 0)
            {
                Console.WriteLine("[UT2004StatsService] No processed stat logs found.");
                return;
            }

            var summarizer = new RuleBasedMatchSummarizer();
            int updated = 0;
            int skipped = 0;

            Console.WriteLine($"[UT2004StatsService] Generating summaries for {allMatches.Count} matches...");

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
                            var resolved = _ratingsMapper.Resolve(p.Guid);
                            if (!profiles.TryGetValue(resolved, out var prof)) continue;

                            double change = match.GameMode switch
                            {
                                UT2004GameMode.iCTF => prof.CaptureTheFlagElo.Change,
                                UT2004GameMode.TAM => prof.TAMElo.Change,
                                UT2004GameMode.iBR => prof.BombingRunElo.Change,
                                _ => 0.0
                            };

                            eloChanges[resolved] = change;
                        }
                    }

                    // Summarize and persist
                    summarizer.Summarize(match, profiles, eloChanges);
                    await _dataContext.SaveStatLogMatchResultFile(match);

                    updated++;
                    if (updated % 100 == 0)
                        Console.WriteLine($"[UT2004StatsService] Summaries updated: {updated}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[UT2004StatsService] Error saving summary for {match.FileName} ({match.MatchDate:yyyy-MM-dd}): {ex.Message}");
                }
            }

            Console.WriteLine($"[UT2004StatsService] Done. Updated: {updated}, Skipped (already present): {skipped}");
        }

        public async Task PrintAllPlayerRatings()
        {
            // Make temp list and sort by most total matches across all modes
            List<UT2004PlayerProfile> tempList = new List<UT2004PlayerProfile>(_dataContext.UT2004PlayerProfileFiles.Select(f => f.PlayerProfile));
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

        #region Admin StatLog Controls
        public async Task<string> GetStatLogIDsOnDate(DateTime date, string serverName = null)
        {
            if (_dataContext.StatLogIndexFile == null)
                await _dataContext.LoadStatLogIndexFile();

            var entries = _dataContext.StatLogIndexFile?.Entries;
            if (entries == null || entries.Count == 0)
                return string.Empty;

            // Use a date range (avoids allocating Date for every entry)
            var start = date.Date;
            var end = start.AddDays(1);

            var sb = new StringBuilder();
            bool matchServer = !string.IsNullOrWhiteSpace(serverName);

            foreach (var e in entries)
            {
                if (e == null) continue;
                var md = e.MatchDate;
                if (md < start || md >= end) continue;
                if (matchServer && !(e.ServerName?.Equals(serverName, StringComparison.OrdinalIgnoreCase) == true)) continue;

                sb.AppendFormat("{0} ({1} - {2:yyyy-MM-dd HH:mm:ss})\n", e.Id, e.ServerName ?? "Unknown Server", md);
            }

            return sb.ToString();
        }

        public async Task<Dictionary<string, string>> GetStatLogByID(string statLogID)
        {
            var log = await _dataContext.LoadStatLogByID(statLogID);
            if (log == null) return null;

            var profileNames = _dataContext.UT2004PlayerProfileFiles?
                .Where(f => f?.PlayerProfile != null && !string.IsNullOrEmpty(f.PlayerProfile.Guid))
                .ToDictionary(
                    f => f.PlayerProfile.Guid,
                    f => f.PlayerProfile.CurrentName,
                    StringComparer.OrdinalIgnoreCase)
                ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            return new Dictionary<string, string>
            {
                [log.Id] = StatLogReader.ReadStatLog(log, profileNames)
            };
        }

        public async Task SendStatLogDM(ulong discordId, string statLogID, string message)
        {
            var bytes = Encoding.UTF8.GetBytes(message ?? string.Empty);
            using var ms = new MemoryStream(bytes) { Position = 0 };

            var user = await _client.GetUserAsync(discordId) as SocketUser;
            if (user == null) return;
            var dmChannel = await user.CreateDMChannelAsync();
            await dmChannel.SendFileAsync(ms, $"{statLogID}.txt", $"Here is the stat log you requested: {statLogID}");
        }

        public async Task<(List<string> Succeeded, List<string> AlreadyIgnored, List<string> NotFound)> IgnoreStatLogsByID(List<string> statLogIDs, ulong adminDiscordId, string adminName)
        {
            if (_dataContext.AdminIgnoredLogsFile == null)
                await _dataContext.LoadAdminIgnoredLogsFile();

            if (_dataContext.StatLogIndexFile == null)
                await _dataContext.LoadStatLogIndexFile();

            var succeeded = new List<string>();
            var alreadyIgnored = new List<string>();
            var notFound = new List<string>();

            var indexedIds = _dataContext.StatLogIndexFile?.Entries
                .Select(e => e.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
                ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var id in statLogIDs)
            {
                if (!indexedIds.Contains(id))
                {
                    notFound.Add(id);
                    continue;
                }

                if (_dataContext.IsStatLogIgnored(id))
                {
                    alreadyIgnored.Add(id);
                    continue;
                }

                await _dataContext.AddAdminIgnoredLogEntry(new AdminIgnoredLogEntry
                {
                    StatLogId = id,
                    AdminDiscordID = adminDiscordId,
                    AdminName = adminName,
                    IgnoredAt = DateTime.UtcNow
                });

                succeeded.Add(id);
            }

            Console.WriteLine($"[UT2004StatsService] IgnoreStatLogsByID — Ignored: {succeeded.Count}, Already ignored: {alreadyIgnored.Count}, Not found: {notFound.Count}");
            return (succeeded, alreadyIgnored, notFound);
        }

        public async Task<(List<string> Succeeded, List<string> AlreadyAllowed, List<string> NotFound)> AllowStatLogsByID(List<string> statLogIDs)
        {
            if (_dataContext.AdminIgnoredLogsFile == null)
                await _dataContext.LoadAdminIgnoredLogsFile();

            if (_dataContext.StatLogIndexFile == null)
                await _dataContext.LoadStatLogIndexFile();

            var succeeded = new List<string>();
            var alreadyAllowed = new List<string>();
            var notFound = new List<string>();

            var indexedIds = _dataContext.StatLogIndexFile?.Entries
                .Select(e => e.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
                ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var id in statLogIDs)
            {
                if (!indexedIds.Contains(id))
                {
                    notFound.Add(id);
                    continue;
                }

                if (!_dataContext.IsStatLogIgnored(id))
                {
                    alreadyAllowed.Add(id);
                    continue;
                }

                await _dataContext.RemoveAdminIgnoredLogEntry(id);
                succeeded.Add(id);
            }

            Console.WriteLine($"[UT2004StatsService] AllowStatLogsByID — Re-allowed: {succeeded.Count}, Already allowed: {alreadyAllowed.Count}, Not found: {notFound.Count}");
            return (succeeded, alreadyAllowed, notFound);
        }
        #endregion
    }
}
