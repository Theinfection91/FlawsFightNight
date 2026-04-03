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
using FlawsFightNight.Services.Logging;
using Discord.WebSocket;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;

namespace FlawsFightNight.Services
{
    public class UT2004StatsService : BaseDataDriven
    {
        private readonly DiscordSocketClient _client;
        private readonly ILogger<UT2004StatsService> _logger;

        private readonly OpenSkillRatingService _ratingService;
        private readonly UTStatsDBEloRatingService _eloService;
        private readonly SeamlessRatingsMapper _ratingsMapper;
        private int _iCTFStatLogIdCounter = 0;
        private int _TAMStatLogIdCounter = 0;
        private int _iBRStatLogIdCounter = 0;

        private const int MinCTFMatchesBeforePeak = 10;
        private const int MinTAMMatchesBeforePeak = 3;
        private const int MinBRMatchesBeforePeak = 3;

        public UT2004StatsService(DataContext dataContext, DiscordSocketClient client, OpenSkillRatingService ratingService, UTStatsDBEloRatingService eloService, SeamlessRatingsMapper ratingsMapper, ILogger<UT2004StatsService> logger) : base("UT2004StatsService", dataContext)
        {
            _client = client;
            _ratingService = ratingService;
            _ratingsMapper = ratingsMapper;
            _eloService = eloService;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            await GetStatLogCounts();
        }

        public bool IsValidGuid(string guid)
        {
            // UT2004 GUIDs are typically 32-character hexadecimal strings
            var uuidGuidPattern = @"^[0-9a-fA-F]{8}[0-9a-fA-F]{4}[0-9a-fA-F]{4}[0-9a-fA-F]{4}[0-9a-fA-F]{12}$";
            return System.Text.RegularExpressions.Regex.IsMatch(guid, uuidGuidPattern);
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

        public async Task<(bool wasValid, string? ignoreReason)> ProcessLogFile(Stream fileStream, string fileName, string serverName, string serverAddress)
        {
            // Instantiate per-call: UT2004LogParser holds mutable parse state and must not be shared
            var parser = new UT2004LogParser();
            var statLog = await parser.Parse<UT2004StatLog>(fileStream);
            if (statLog != null)
            {
                statLog.FileName = Path.ChangeExtension(fileName, ".json");
                statLog.ServerName = serverName;
                statLog.IPAddress = serverAddress;
                statLog.Players = statLog.Players.Select(teamList =>
                    teamList.OrderByDescending(p => p.Score).ToList()
                ).ToList();
                statLog.Id = GenerateStatLogId(statLog.GameMode);

                if (_dataContext.StatLogIndexFile == null)
                    await _dataContext.LoadStatLogIndexFile();

                var savedTag = _dataContext.GetTournamentStatTag(statLog.FileName);
                statLog.IsAllowedByAdmin = savedTag?.IsAdminIgnored != true;

                await _dataContext.SaveStatLogMatchResultFile(statLog);

                var entry = new StatLogIndexEntry
                {
                    Id = statLog.Id,
                    MatchDate = statLog.MatchDate,
                    ServerName = statLog.ServerName
                };

                if (savedTag != null)
                {
                    if (!string.IsNullOrEmpty(savedTag.TournamentId))
                        entry.TagTournamentMatch(savedTag.TournamentId, savedTag.MatchId, savedTag.TournamentName);

                    if (savedTag.IsAdminIgnored)
                    {
                        entry.IsAdminIgnored = true;
                        entry.AdminDiscordID = savedTag.AdminDiscordID;
                        entry.AdminName = savedTag.AdminName;
                        entry.IgnoredAt = savedTag.IgnoredAt ?? DateTime.UtcNow;
                    }

                    savedTag.StatLogId = statLog.Id;
                    await _dataContext.UpsertTournamentStatTag(savedTag);
                }

                await _dataContext.AddStatLogIndexEntry(entry);
                await MarkLogFileAsProcessed(fileName);
                return (true, null);
            }
            else
            {
                await MarkLogFileAsIgnored(fileName);
                return (false, parser.LastIgnoreReason);
            }
        }

        public async Task GetStatLogCounts()
        {
            _iCTFStatLogIdCounter = await _dataContext.GetStatLogCount(UT2004GameMode.iCTF);
            _TAMStatLogIdCounter = await _dataContext.GetStatLogCount(UT2004GameMode.TAM);
            _iBRStatLogIdCounter = await _dataContext.GetStatLogCount(UT2004GameMode.iBR);
        }

        public string GenerateStatLogId(UT2004GameMode gameMode)
        {
            int newCount = gameMode switch
            {
                UT2004GameMode.iCTF => Interlocked.Increment(ref _iCTFStatLogIdCounter),
                UT2004GameMode.TAM => Interlocked.Increment(ref _TAMStatLogIdCounter),
                UT2004GameMode.iBR => Interlocked.Increment(ref _iBRStatLogIdCounter),
                _ => 0
            };

            return $"{gameMode}{newCount:000000}";
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

            // Ensure admin ignore file is loaded so we can mark each StatLog correctly
            if (_dataContext.StatLogIndexFile == null)
                await _dataContext.LoadStatLogIndexFile();

            var list = new List<UT2004StatLog>();
            foreach (var file in statLogFiles)
            {
                var statLog = file.StatLog;
                if (statLog == null) continue;

                // Ensure the persisted StatLog's IsAllowedByAdmin reflects the admin ignore list
                statLog.IsAllowedByAdmin = !IsStatLogIgnored(statLog.Id);
                list.Add(statLog);
            }

            return list;
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
                _logger.LogInformation("SeamlessRatings: active aliases detected — merged GUIDs will be treated as one identity.");
                foreach (var profile in memberProfiles.Where(p => p.RegisteredUT2004GUIDs?.Count >= 2))
                {
                    string primary = profile.RegisteredUT2004GUIDs[0];
                    _logger.LogDebug("SeamlessRatings player: {DisplayName} | Primary GUID: {PrimaryGuid}", profile.DisplayName, primary);
                    foreach (var guid in profile.RegisteredUT2004GUIDs.Skip(1))
                        _logger.LogDebug("SeamlessRatings alias: {Guid} → {PrimaryGuid}", guid, primary);
                }
            }
            else
            {
                _logger.LogInformation("SeamlessRatings: no aliases active — all GUIDs treated independently.");
            }

            // Ensure the admin ignored logs file is loaded before filtering
            if (_dataContext.StatLogIndexFile == null)
                await _dataContext.LoadStatLogIndexFile();

            var allMatchStats = await GetAllProcessedStatLogs();

            var chronologicalMatches = allMatchStats
                .Where(m => !IsStatLogIgnored(m.Id) && m.IsAllowedByAdmin)
                .OrderBy(m => m.MatchDate)
                .ToList();

            int ignoredCount = allMatchStats.Count - chronologicalMatches.Count;
            if (ignoredCount > 0)
                _logger.LogInformation("Skipping {IgnoredCount} admin-ignored stat log(s) from profile calculations.", ignoredCount);

            _logger.LogInformation("Processing {MatchCount} matches chronologically...", chronologicalMatches.Count);

            if (chronologicalMatches.Count == 0)
            {
                _logger.LogInformation("No matches to process.");
                return;
            }

            _logger.LogInformation("Date range: {Start:yyyy-MM-dd} to {End:yyyy-MM-dd}", chronologicalMatches.First().MatchDate, chronologicalMatches.Last().MatchDate);

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
                            _logger.LogInformation("SeamlessRatings first merge: raw GUID {RawGuid} → primary {PrimaryGuid} (match: {FileName}, date: {MatchDate:yyyy-MM-dd})", playerStats.Guid, resolvedGuid, match.FileName, match.MatchDate);
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
                    _logger.LogError(ex, "Error generating/saving match summary for {FileName} on {MatchDate:yyyy-MM-dd}.", match.FileName, match.MatchDate);
                }

                if (processedCount % 100 == 0)
                    _logger.LogInformation("Processed {Processed}/{Total} matches...", processedCount, chronologicalMatches.Count);
            }

            if (mergeLog.Count > 0)
            {
                _logger.LogInformation("SeamlessRatings: {MergeCount} alias GUID(s) merged during replay.", mergeLog.Count);
                var affectedPrimaries = mergeLog.Values.Distinct().ToList();
                foreach (var primaryGuid in affectedPrimaries)
                {
                    if (!profiles.TryGetValue(primaryGuid, out var merged)) continue;
                    _logger.LogInformation(
                        "SeamlessRatings merged profile — GUID: {PrimaryGuid} | Name: {Name} | CTF ELO: {CtfElo:F1} | TAM ELO: {TamElo:F1} | BR ELO: {BrElo:F1} | Matches: CTF={CtfMatches} TAM={TamMatches} BR={BrMatches}",
                        primaryGuid, merged.CurrentName,
                        merged.CaptureTheFlagElo.Rating, merged.TAMElo.Rating, merged.BombingRunElo.Rating,
                        merged.TotalCTFMatches, merged.TotalTAMMatches, merged.TotalBRMatches);
                }
            }

            _logger.LogInformation("Saving {ProfileCount} player profiles...", profiles.Count);
            _logger.LogInformation(AdminFeedEvents.PlayerProfilesRebuilt, "{ProfileCount} player profiles have been rebuilt in the database. {AliasCount} of those profiles are aliases merged into their primary GUID's profile for SeamlessRatings.", profiles.Count, mergeLog.Count);

            await GenerateAndPersistMatchSummaries();

            foreach (var profile in profiles.Values)
                await _dataContext.SaveUT2004PlayerProfileFile(profile);

            // Save standalone raw profiles for aliased GUIDs
            if (rawProfiles.Count > 0)
            {
                _logger.LogInformation("SeamlessRatings: saving {RawProfileCount} standalone raw GUID profile(s).", rawProfiles.Count);
                foreach (var rawProfile in rawProfiles.Values)
                    await _dataContext.SaveUT2004PlayerProfileFile(rawProfile);
            }

            await _dataContext.LoadAllUT2004PlayerProfileFiles();
        }

        public async Task RebuildPlayerProfiles()
        {
            _logger.LogInformation("Rebuilding player profiles from scratch...");

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
                _logger.LogWarning("No processed stat logs found.");
                return;
            }

            var summarizer = new RuleBasedMatchSummarizer();
            int updated = 0;
            int skipped = 0;

            _logger.LogInformation("Generating summaries for {MatchCount} matches...", allMatches.Count);

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
                        _logger.LogInformation("Summaries updated: {Updated}.", updated);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving summary for {FileName} ({MatchDate:yyyy-MM-dd}).", match.FileName, match.MatchDate);
                }
            }

            _logger.LogInformation("Summaries complete. Updated: {Updated}, Skipped (already present): {Skipped}.", updated, skipped);
        }

        public async Task PrintAllPlayerRatings()
        {
            // Make temp list and sort by most total matches across all modes
            List<UT2004PlayerProfile> tempList = new List<UT2004PlayerProfile>(_dataContext.UT2004PlayerProfileFiles.Select(f => f.PlayerProfile));
            tempList = tempList.OrderByDescending(p => p.TotalMatches).ToList();
            foreach (var profile in tempList)
            {
                _logger.LogInformation(
                    "Player {Guid}: CTF ELO {CtfElo:F1} (Δ{CtfChange:+0.0;-0.0} Peak {CtfPeak:F1} on {CtfPeakDate:yyyy-MM-dd}) | TAM ELO {TamElo:F1} (Δ{TamChange:+0.0;-0.0} Peak {TamPeak:F1} on {TamPeakDate:yyyy-MM-dd}) | BR ELO {BrElo:F1} (Δ{BrChange:+0.0;-0.0} Peak {BrPeak:F1} on {BrPeakDate:yyyy-MM-dd}) | Matches CTF={CtfMatches} TAM={TamMatches} BR={BrMatches}",
                    profile.Guid,
                    profile.CaptureTheFlagElo.Rating, profile.CaptureTheFlagElo.Change, profile.CaptureTheFlagElo.Peak, profile.CaptureTheFlagElo.PeakDate,
                    profile.TAMElo.Rating, profile.TAMElo.Change, profile.TAMElo.Peak, profile.TAMElo.PeakDate,
                    profile.BombingRunElo.Rating, profile.BombingRunElo.Change, profile.BombingRunElo.Peak, profile.BombingRunElo.PeakDate,
                    profile.TotalCTFMatches, profile.TotalTAMMatches, profile.TotalBRMatches);
            }
        }
        #endregion

        #region Admin StatLog Controls
        public bool IsStatLogIgnored(string statLogID)
        {
            return _dataContext.StatLogIndexFile.Entries.Any(e => e.Id.Equals(statLogID, StringComparison.OrdinalIgnoreCase) && e.IsAdminIgnored);
        }

        public List<string> GetAdminIgnoredLogs()
        {
            if (_dataContext.StatLogIndexFile == null) return new List<string>();
            return _dataContext.StatLogIndexFile.Entries.Where(e => e.IsAdminIgnored).Select(e => e.Id).ToList();
        }

        public List<StatLogIndexEntry> GetAdminIgnoredLogEntries()
        {
            if (_dataContext.StatLogIndexFile == null) return new List<StatLogIndexEntry>();
            return _dataContext.StatLogIndexFile.Entries.Where(e => e.IsAdminIgnored).ToList();
        }

        public bool DoesStatLogExist(string statLogID)
        {
            var entries = _dataContext.StatLogIndexFile?.Entries;
            return entries != null && entries.Any(e => e.Id.Equals(statLogID, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Returns the canonical (stored) stat log ID for the given input, ignoring case.
        /// Returns null if no matching entry exists.
        /// </summary>
        public string? TryResolveStatLogId(string input)
        {
            var entries = _dataContext.StatLogIndexFile?.Entries;
            return entries?.FirstOrDefault(e => e.Id.Equals(input, StringComparison.OrdinalIgnoreCase))?.Id;
        }

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

        /// <summary>
        /// Return the last N indexed stat log IDs optionally filtered by server name (most recent first).
        /// </summary>
        public async Task<string> GetLastStatLogIDs(int amount, string serverName = null)
        {
            if (amount <= 0) return string.Empty;

            if (_dataContext.StatLogIndexFile == null)
                await _dataContext.LoadStatLogIndexFile();

            var entries = _dataContext.StatLogIndexFile?.Entries;
            if (entries == null || entries.Count == 0)
                return string.Empty;

            var filtered = entries
                .Where(e => string.IsNullOrWhiteSpace(serverName) || (e.ServerName?.Equals(serverName, StringComparison.OrdinalIgnoreCase) == true))
                .OrderByDescending(e => e.MatchDate)
                .Take(amount)
                .ToList();

            if (filtered.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            foreach (var e in filtered)
            {
                var ignored = IsStatLogIgnored(e.Id) ? " [IGNORED]" : "";
                sb.AppendFormat("{0} ({1} - {2:yyyy-MM-dd HH:mm:ss}){3}\n", e.Id, e.ServerName ?? "Unknown Server", e.MatchDate, ignored);
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

        public async Task<string> GetStatLogMatchSummary(string statLogID)
        {
            var log = await _dataContext.LoadStatLogByID(statLogID);
            if (log == null) return null;
            return log.MatchSummary!;
        }

        public async Task SendStatLogDM(ulong discordId, Dictionary<string, string> statLogs)
        {
            if (statLogs == null || statLogs.Count == 0) return;

            var user = await _client.GetUserAsync(discordId) as SocketUser;
            if (user == null) return;

            var dmChannel = await user.CreateDMChannelAsync();

            const int MaxFilesPerMessage = 10;
            var entries = statLogs.ToList();

            for (int i = 0; i < entries.Count; i += MaxFilesPerMessage)
            {
                var batch = entries.Skip(i).Take(MaxFilesPerMessage).ToList();
                var attachments = new List<FileAttachment>();

                try
                {
                    foreach (var kvp in batch)
                    {
                        var bytes = Encoding.UTF8.GetBytes(kvp.Value ?? string.Empty);
                        attachments.Add(new FileAttachment(new MemoryStream(bytes), $"{kvp.Key}.txt"));
                    }

                    string caption = batch.Count == 1
                        ? $"Here is the stat log you requested: {batch[0].Key}"
                        : $"Here are {batch.Count} stat log(s) you requested.";

                    await dmChannel.SendFilesAsync(attachments, caption);
                }
                finally
                {
                    foreach (var attachment in attachments)
                        attachment.Dispose();
                }
            }
        }

        public async Task SendMatchSummaryToChannel(SocketInteractionContext context, string statLogID)
        {
            var matchSummary = await GetStatLogMatchSummary(statLogID);
            if (matchSummary == null) return;
            var bytes = Encoding.UTF8.GetBytes(matchSummary);
            var attachment = new FileAttachment(new MemoryStream(bytes), $"{statLogID}_summary.txt");
            try
            {
                await context.Channel.SendFileAsync(attachment);
            }
            finally
            {
                attachment.Dispose();
            }
        }

        public async Task<(List<string> Succeeded, List<string> AlreadyIgnored, List<string> NotFound)> IgnoreStatLogsByID(List<string> statLogIDs, ulong adminDiscordId, string adminName)
        {
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

                if (IsStatLogIgnored(id))
                {
                    alreadyIgnored.Add(id);
                    continue;
                }

                var logIndex = _dataContext.GetStatLogIndexEntry(id);
                logIndex.IsAdminIgnored = true;
                logIndex.AdminDiscordID = adminDiscordId;
                logIndex.AdminName = adminName;
                logIndex.IgnoredAt = DateTime.UtcNow;
                await _dataContext.SaveAndReloadStatLogIndexFile();

                // Persist IsAllowedByAdmin on the actual stat log file so future loads reflect admin intent
                // Also upsert into tournament_stat_tags.json so the ignore survives a backup restore
                var statLog = await _dataContext.LoadStatLogByID(id);
                if (statLog != null)
                {
                    statLog.IsAllowedByAdmin = false;
                    await _dataContext.SaveStatLogMatchResultFile(statLog);

                    var tag = _dataContext.GetTournamentStatTag(statLog.FileName) ?? new TournamentStatTag
                    {
                        StatLogFileName = statLog.FileName,
                        MatchDate = logIndex.MatchDate,
                        ServerName = logIndex.ServerName
                    };
                    tag.StatLogId = id;
                    tag.IsAdminIgnored = true;
                    tag.AdminDiscordID = adminDiscordId;
                    tag.AdminName = adminName;
                    tag.IgnoredAt = DateTime.UtcNow;
                    await _dataContext.UpsertTournamentStatTag(tag);
                }

                succeeded.Add(id);
            }

            _logger.LogInformation(AdminFeedEvents.AdminActionTaken, "Stat logs ignored: {Succeeded}, Already ignored: {AlreadyIgnored}, Not found: {NotFound}.", succeeded.Count, alreadyIgnored.Count, notFound.Count);
            return (succeeded, alreadyIgnored, notFound);
        }

        public async Task<(List<string> Succeeded, List<string> AlreadyAllowed, List<string> NotFound)> AllowStatLogsByID(List<string> statLogIDs)
        {
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

                if (!IsStatLogIgnored(id))
                {
                    alreadyAllowed.Add(id);
                    continue;
                }

                var logIndex = _dataContext.GetStatLogIndexEntry(id);
                if (logIndex != null)
                {
                    logIndex.IsAdminIgnored = false;
                    logIndex.AdminDiscordID = 0;
                    logIndex.AdminName = null;
                    logIndex.IgnoredAt = DateTime.MinValue;
                }

                await _dataContext.SaveAndReloadStatLogIndexFile();

                // Persist to tournament_stat_tags.json
                var statLog = await _dataContext.LoadStatLogByID(id);
                if (statLog != null)
                {
                    var tag = _dataContext.GetTournamentStatTag(statLog.FileName);
                    if (tag != null)
                    {
                        if (!string.IsNullOrEmpty(tag.TournamentId))
                        {
                            tag.IsAdminIgnored = false;
                            tag.AdminDiscordID = 0;
                            tag.AdminName = null;
                            tag.IgnoredAt = null;
                            await _dataContext.UpsertTournamentStatTag(tag);
                        }
                        else
                        {
                            await _dataContext.RemoveTournamentStatTag(statLog.FileName);
                        }
                    }

                    statLog.IsAllowedByAdmin = true;
                    await _dataContext.SaveStatLogMatchResultFile(statLog);
                }

                succeeded.Add(id);
            }

            _logger.LogInformation(AdminFeedEvents.AdminActionTaken, "Stat logs re-allowed: {Succeeded}, Already allowed: {AlreadyAllowed}, Not found: {NotFound}.", succeeded.Count, alreadyAllowed.Count, notFound.Count);
            return (succeeded, alreadyAllowed, notFound);
        }

        public bool IsTournamentMatchTagged(string tournamentId, string matchId)
        {
            var entries = _dataContext.StatLogIndexFile?.Entries;
            if (entries == null) return false;
            return entries.Any(e =>
                e.TournamentId != null && e.TournamentId.Equals(tournamentId, StringComparison.OrdinalIgnoreCase) &&
                e.MatchId != null && e.MatchId.Equals(matchId, StringComparison.OrdinalIgnoreCase));
        }

        public bool IsStatLogTaggedToTournamentMatch(string statLogID)
        {
            var entries = _dataContext.StatLogIndexFile?.Entries;
            if (entries == null) return false;
            return entries.Any(e =>
                e.Id.Equals(statLogID, StringComparison.OrdinalIgnoreCase) && e.IsTagged);
        }

        public StatLogIndexEntry GetStatLogIndexEntry(string statLogID)
        {
            return _dataContext.StatLogIndexFile?.Entries
                .FirstOrDefault(e => e.Id.Equals(statLogID, StringComparison.OrdinalIgnoreCase))!;
        }

        public StatLogIndexEntry GetStatLogIndexEntryByTournamentMatch(string tournamentId, string matchId)
        {
            return _dataContext.StatLogIndexFile?.Entries.FirstOrDefault(e =>
                e.TournamentId != null && e.TournamentId.Equals(tournamentId, StringComparison.OrdinalIgnoreCase) &&
                e.MatchId != null && e.MatchId.Equals(matchId, StringComparison.OrdinalIgnoreCase))!;
        }

        public async Task TagTournamentMatchToStatLog(string statLogID, string tournamentName, string tournamentID, string matchID)
        {
            var logIndex = _dataContext.GetStatLogIndexEntry(statLogID);
            if (logIndex == null) return;

            logIndex.TagTournamentMatch(tournamentID, matchID, tournamentName);
            await _dataContext.SaveAndReloadStatLogIndexFile();

            // Persist to tournament_stat_tags.json so the tag survives a backup restore
            var statLog = await _dataContext.LoadStatLogByID(statLogID);
            if (statLog != null)
            {
                var tag = _dataContext.GetTournamentStatTag(statLog.FileName) ?? new TournamentStatTag
                {
                    StatLogFileName = statLog.FileName,
                    MatchDate = logIndex.MatchDate,
                    ServerName = logIndex.ServerName
                };
                tag.StatLogId = statLogID;
                tag.TournamentId = tournamentID;
                tag.MatchId = matchID;
                tag.TournamentName = tournamentName;
                tag.TaggedAt = DateTime.UtcNow;
                await _dataContext.UpsertTournamentStatTag(tag);
            }
        }

        public async Task UnTagTournamentMatchFromStatLog(string statLogID)
        {
            var logIndex = _dataContext.GetStatLogIndexEntry(statLogID);
            if (logIndex == null) return;

            logIndex.UnTagTournamentMatch();
            await _dataContext.SaveAndReloadStatLogIndexFile();

            // Update or remove from tournament_stat_tags.json
            var statLog = await _dataContext.LoadStatLogByID(statLogID);
            if (statLog != null)
            {
                var tag = _dataContext.GetTournamentStatTag(statLog.FileName);
                if (tag != null)
                {
                    if (tag.IsAdminIgnored)
                    {
                        // Still has ignore state — only clear tournament fields
                        tag.TournamentId = string.Empty;
                        tag.MatchId = string.Empty;
                        tag.TournamentName = null;
                        tag.TaggedAt = default;
                        await _dataContext.UpsertTournamentStatTag(tag);
                    }
                    else
                    {
                        // No remaining admin state — remove the entry entirely
                        await _dataContext.RemoveTournamentStatTag(statLog.FileName);
                    }
                }
            }
        }
        #endregion

        #region User Query Methods
        public List<UT2004PlayerProfile> GetAllPrimaryPlayerProfiles()
        {
            return _dataContext.UT2004PlayerProfileFiles
                .Where(f => f?.PlayerProfile != null && !_ratingsMapper.IsAlias(f.PlayerProfile.Guid))
                .Select(f => f.PlayerProfile)
                .ToList();
        }

        public List<UT2004PlayerProfile> GetAllPlayerProfiles()
        {
            return _dataContext.UT2004PlayerProfileFiles
                .Where(f => f?.PlayerProfile != null)
                .Select(f => f.PlayerProfile)
                .ToList();
        }

        public UT2004PlayerProfile? GetPlayerProfileByGuid(string guid)
        {
            return _dataContext.UT2004PlayerProfileFiles
                .Where(f => f?.PlayerProfile != null)
                .Select(f => f.PlayerProfile)
                .FirstOrDefault(p => p.Guid.Equals(guid, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<List<string>> GetTournamentStatLogIdsByGuids(List<string> guids)
        {
            if (_dataContext.StatLogIndexFile == null)
                await _dataContext.LoadStatLogIndexFile();

            var taggedEntries = _dataContext.StatLogIndexFile.Entries
                .Where(e => e.IsTagged && !e.IsAdminIgnored)
                .ToList();

            if (taggedEntries.Count == 0) return new List<string>();

            var guidSet = new HashSet<string>(guids, StringComparer.OrdinalIgnoreCase);
            var results = new List<string>();

            foreach (var entry in taggedEntries.OrderByDescending(e => e.MatchDate))
            {
                var statLog = await _dataContext.LoadStatLogByID(entry.Id);
                if (statLog == null) continue;

                bool hasGuid = statLog.Players.Any(team =>
                    team.Any(p => !string.IsNullOrEmpty(p.Guid) && guidSet.Contains(p.Guid)));
                if (hasGuid)
                {
                    results.Add($"{entry.Id} ({entry.TournamentName ?? "Unknown"} - Match: {entry.MatchId ?? "N/A"} - {entry.MatchDate:yyyy-MM-dd})");
                }
            }

            return results;
        }

        public async Task<List<string>> GetAllStatLogIdsByGuids(List<string> guids)
        {
            if (_dataContext.StatLogIndexFile == null)
                await _dataContext.LoadStatLogIndexFile();

            var entries = _dataContext.StatLogIndexFile!.Entries
                .Where(e => !e.IsAdminIgnored)
                .OrderByDescending(e => e.MatchDate)
                .ToList();

            if (entries.Count == 0) return new List<string>();

            var guidSet = new HashSet<string>(guids, StringComparer.OrdinalIgnoreCase);
            var results = new List<string>();

            foreach (var entry in entries)
            {
                var statLog = await _dataContext.LoadStatLogByID(entry.Id);
                if (statLog == null) continue;

                bool hasGuid = statLog.Players.Any(team =>
                    team.Any(p => !string.IsNullOrEmpty(p.Guid) && guidSet.Contains(p.Guid)));
                if (hasGuid)
                {
                    results.Add($"{statLog.Id} ({statLog.GameModeName} - {statLog.ServerName ?? "Unknown"} - {statLog.MatchDate:yyyy-MM-dd HH:mm:ss})");
                }
            }

            return results;
        }
        #endregion

        public async Task SendTextFileDM(ulong discordId, string fileName, string content)
        {
            var user = await _client.GetUserAsync(discordId) as SocketUser;
            if (user == null) return;

            var dmChannel = await user.CreateDMChannelAsync();
            var bytes = Encoding.UTF8.GetBytes(content);
            var attachment = new FileAttachment(new MemoryStream(bytes), fileName);
            try
            {
                await dmChannel.SendFileAsync(attachment);
            }
            finally
            {
                attachment.Dispose();
            }
        }

        #region Elo Tracing
        private record EloTraceEntry(
            DateTime MatchDate,
            string StatLogId,
            string ServerName,
            string PlayerName,
            int Score,
            int Kills,
            int Deaths,
            bool IsWinner,
            int ObjStat,
            double EloBefore,
            double EloAfter,
            double Change);

        /// <summary>
        /// Replays all <paramref name="gameMode"/> matches chronologically and returns a
        /// formatted per-match ELO trace for the given GUID.
        /// Alias GUIDs are resolved to their primary before the replay.
        /// Opponent ratings are accurate because every human player is carried through the
        /// replay — only the isolated profiles dictionary is mutated, not the live database.
        /// </summary>
        public async Task<string> GetPlayerEloTrace(string guid, UT2004GameMode gameMode)
        {
            var resolvedGuid = _ratingsMapper.Resolve(guid);

            if (_dataContext.StatLogIndexFile == null)
                await _dataContext.LoadStatLogIndexFile();

            var allMatchStats = await GetAllProcessedStatLogs();

            var chronologicalMatches = allMatchStats
                .Where(m => !IsStatLogIgnored(m.Id) && m.IsAllowedByAdmin && m.GameMode == gameMode)
                .OrderBy(m => m.MatchDate)
                .ToList();

            if (chronologicalMatches.Count == 0)
                return $"No {gameMode} matches found in the database.";

            // Isolated replay — never touches live database profiles
            var profiles = new Dictionary<string, UT2004PlayerProfile>(StringComparer.OrdinalIgnoreCase);
            var traceEntries = new List<EloTraceEntry>();

            foreach (var match in chronologicalMatches)
            {
                // Seed profiles for every human player in this match
                foreach (var team in match.Players)
                {
                    foreach (var p in team.Where(p => !p.IsBot && !string.IsNullOrEmpty(p.Guid)))
                    {
                        var rg = _ratingsMapper.Resolve(p.Guid!);
                        if (!profiles.ContainsKey(rg))
                            profiles[rg] = new UT2004PlayerProfile(rg);
                    }
                }

                // Find target player's stats in this match before mutating anything
                UTPlayerMatchStats? targetStats = null;
                foreach (var team in match.Players)
                {
                    targetStats = team.FirstOrDefault(p =>
                        !p.IsBot &&
                        !string.IsNullOrEmpty(p.Guid) &&
                        _ratingsMapper.Resolve(p.Guid!).Equals(resolvedGuid, StringComparison.OrdinalIgnoreCase));
                    if (targetStats != null) break;
                }

                // Snapshot ELO before this match
                double eloBefore = 0.0;
                if (targetStats != null && profiles.TryGetValue(resolvedGuid, out var tpSnap))
                {
                    eloBefore = gameMode switch
                    {
                        UT2004GameMode.iCTF => tpSnap.CaptureTheFlagElo.Rating,
                        UT2004GameMode.TAM => tpSnap.TAMElo.Rating,
                        UT2004GameMode.iBR => tpSnap.BombingRunElo.Rating,
                        _ => 0.0
                    };
                }

                // Stats must be updated before ELO so MinRankMatches gating mirrors SetupPlayerProfiles
                foreach (var team in match.Players)
                {
                    foreach (var p in team.Where(p => !p.IsBot && !string.IsNullOrEmpty(p.Guid)))
                    {
                        var rg = _ratingsMapper.Resolve(p.Guid!);
                        profiles[rg].UpdateStatsFromMatch(p, match.GameMode, match.MatchDate);
                    }
                }

                _eloService.UpdateRatingsForMatch(match, profiles);

                if (targetStats != null && profiles.TryGetValue(resolvedGuid, out var tpAfter))
                {
                    double eloAfter = gameMode switch
                    {
                        UT2004GameMode.iCTF => tpAfter.CaptureTheFlagElo.Rating,
                        UT2004GameMode.TAM => tpAfter.TAMElo.Rating,
                        UT2004GameMode.iBR => tpAfter.BombingRunElo.Rating,
                        _ => 0.0
                    };

                    int objStat = gameMode switch
                    {
                        UT2004GameMode.iCTF => targetStats.FlagCaptures,
                        UT2004GameMode.iBR => targetStats.BallCaptures,
                        UT2004GameMode.TAM => targetStats.TotalDamageDealt,
                        _ => 0
                    };

                    traceEntries.Add(new EloTraceEntry(
                        MatchDate: match.MatchDate,
                        StatLogId: match.Id,
                        ServerName: match.ServerName ?? "Unknown",
                        PlayerName: targetStats.LastKnownName ?? resolvedGuid,
                        Score: targetStats.Score,
                        Kills: targetStats.Kills,
                        Deaths: targetStats.Deaths,
                        IsWinner: targetStats.IsWinner,
                        ObjStat: objStat,
                        EloBefore: eloBefore,
                        EloAfter: eloAfter,
                        Change: eloAfter - eloBefore
                    ));
                }
            }

            if (traceEntries.Count == 0)
                return $"No {gameMode} matches found for GUID {resolvedGuid}.";

            profiles.TryGetValue(resolvedGuid, out var finalProfile);

            double currentElo = finalProfile != null ? gameMode switch
            {
                UT2004GameMode.iCTF => finalProfile.CaptureTheFlagElo.Rating,
                UT2004GameMode.TAM => finalProfile.TAMElo.Rating,
                UT2004GameMode.iBR => finalProfile.BombingRunElo.Rating,
                _ => 0.0
            } : 0.0;

            double peakElo = finalProfile != null ? gameMode switch
            {
                UT2004GameMode.iCTF => finalProfile.CaptureTheFlagElo.Peak,
                UT2004GameMode.TAM => finalProfile.TAMElo.Peak,
                UT2004GameMode.iBR => finalProfile.BombingRunElo.Peak,
                _ => 0.0
            } : 0.0;

            DateTime? peakDate = finalProfile != null ? gameMode switch
            {
                UT2004GameMode.iCTF => finalProfile.CaptureTheFlagElo.PeakDate,
                UT2004GameMode.TAM => finalProfile.TAMElo.PeakDate,
                UT2004GameMode.iBR => finalProfile.BombingRunElo.PeakDate,
                _ => null
            } : null;

            string displayName = traceEntries[^1].PlayerName;
            int wins = traceEntries.Count(e => e.IsWinner);
            int losses = traceEntries.Count - wins;
            double startElo = traceEntries[0].EloBefore;
            double netChange = currentElo - startElo;

            string objLabel = gameMode switch
            {
                UT2004GameMode.iCTF => "FC",
                UT2004GameMode.iBR => "BC",
                UT2004GameMode.TAM => "DMG",
                _ => "Obj"
            };

            const string sep = "--------------------------------------------------------------------------------------------------------------------------------------";

            var sb = new StringBuilder();
            sb.AppendLine($"=== ELO Trace — {displayName} | {gameMode} ===");
            sb.AppendLine($"GUID      : {resolvedGuid}");
            if (!resolvedGuid.Equals(guid, StringComparison.OrdinalIgnoreCase))
                sb.AppendLine($"Alias GUID: {guid}");
            sb.AppendLine($"Matches   : {traceEntries.Count} ({wins}W / {losses}L)");
            sb.AppendLine($"ELO Now   : {currentElo:F1}   |   Peak: {peakElo:F1}{(peakDate.HasValue ? $" ({peakDate.Value:yyyy-MM-dd})" : "")}");
            sb.AppendLine($"Net Delta : {netChange:+0.0;-0.0}   (start: {startElo:F1}  →  end: {currentElo:F1})");
            sb.AppendLine();
            sb.AppendLine($"{"#",-5} {"Date",-12} {"Stat Log ID",-14} {"Server",-24} {"Name",-20} {"R",-3} {"Score",-7} {"K/D",-9} {objLabel,-8} {"Before",-10} {"Δ",-10} After");
            sb.AppendLine(sep);

            int matchNum = 0;
            foreach (var e in traceEntries)
            {
                matchNum++;
                string server = e.ServerName.Length > 22 ? e.ServerName[..22] : e.ServerName;
                string name = e.PlayerName.Length > 18 ? e.PlayerName[..18] : e.PlayerName;
                string kd = $"{e.Kills}/{e.Deaths}";
                string obj = $"{objLabel}:{e.ObjStat}";
                string change = e.Change >= 0.0 ? $"+{e.Change:F1}" : $"{e.Change:F1}";
                string note = Math.Abs(e.Change) < 0.001 ? " [no change]" : string.Empty;

                sb.AppendLine($"{matchNum,-5} {e.MatchDate:yyyy-MM-dd}  {e.StatLogId,-14} {server,-24} {name,-20} {(e.IsWinner ? "W" : "L"),-3} {e.Score,-7} {kd,-9} {obj,-8} {e.EloBefore,-10:F1} {change,-10} {e.EloAfter:F1}{note}");
            }

            sb.AppendLine(sep);
            sb.AppendLine($"{"TOTAL",-5} {"",-12} {"",-14} {"",-24} {"",-20} {$"{wins}W/{losses}L",-3} {"",-7} {"",-9} {"",-8} {startElo,-10:F1} {$"{netChange:+0.0;-0.0}",-10} {currentElo:F1}");

            return sb.ToString();
        }
        #endregion
    }
}
