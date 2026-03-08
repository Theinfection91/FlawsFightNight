using FlawsFightNight.Core.Enums.UT2004;
using FlawsFightNight.Core.Models.UT2004;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FlawsFightNight.Core.Helpers.UT2004
{
    public static class StatLogReader
    {
        private const string Divider     = "================================================";
        private const string ThinDivider = "------------------------------------------------";

        /// <summary>
        /// Returns a formatted, human-readable string of a <see cref="UT2004StatLog"/>.
        /// </summary>
        /// <param name="profileNames">
        /// Optional GUID → display-name fallback map (e.g. from <c>UT2004PlayerProfile.CurrentName</c>).
        /// Used when a player's <c>LastKnownName</c> is null or empty in the stored log.
        /// </param>
        public static string ReadStatLog(UT2004StatLog log, IReadOnlyDictionary<string, string>? profileNames = null)
        {
            var sb = new StringBuilder();

            if (log.IsAllowedByAdmin == false) WriteIgnoreWarning(sb);
            WriteHeader(log, sb);
            WriteTeams(log, sb, profileNames);
            WriteKillMatrix(log, sb, profileNames);
            WriteTimeline(log, sb, profileNames);

            return sb.ToString();
        }

        /// <summary>
        /// Returns a UTF-8 encoded <see cref="MemoryStream"/> of the formatted stat log,
        /// ready to be sent as a .txt file attachment.
        /// </summary>
        /// <param name="profileNames">
        /// Optional GUID → display-name fallback map. See <see cref="ReadStatLog"/>.
        /// </param>
        public static MemoryStream ReadStatLogAsStream(UT2004StatLog log, IReadOnlyDictionary<string, string>? profileNames = null)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(ReadStatLog(log, profileNames));
            return new MemoryStream(bytes) { Position = 0 };
        }

        // ── Section writers ──────────────────────────────────────────────────────

        private static void WriteIgnoreWarning(StringBuilder sb)
        {
            sb.AppendLine(Divider);
            sb.AppendLine("!!!  !  !!!\n");
            sb.AppendLine("  WARNING: MATCH LOG IGNORED BY ADMIN\n");
            sb.AppendLine("This match log is marked as IGNORED by an admin and does not contribute toward stats or ratings currently.");
            sb.AppendLine("  WARNING: MATCH LOG IGNORED BY ADMIN\n");
            sb.AppendLine("!!!  !  !!!");
            sb.AppendLine(Divider);
            sb.AppendLine();
        }

        private static void WriteHeader(UT2004StatLog log, StringBuilder sb)
        {
            sb.AppendLine(Divider);
            sb.AppendLine("  UT2004 STAT LOG");
            sb.AppendLine($"  ID: {log.Id}");
            sb.AppendLine(Divider);
            sb.AppendLine($"  Server    : {log.ServerName ?? "N/A"} ({log.IPAddress ?? "N/A"})");
            sb.AppendLine($"  Match Date: {log.MatchDate:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine($"  Game Mode : {log.GameModeName}");
            sb.AppendLine($"  File      : {log.FileName ?? "N/A"}");
            sb.AppendLine($"  Summary   : {log.MatchSummary ?? "N/A"}");
            sb.AppendLine();
        }

        private static void WriteTeams(UT2004StatLog log, StringBuilder sb, IReadOnlyDictionary<string, string>? profileNames)
        {
            if (log.Players == null || log.Players.Count == 0)
            {
                sb.AppendLine("  No player data available.");
                return;
            }

            // If no player in the entire match is marked as a winner it's a draw (e.g. 0-0 map change)
            bool anyWinner = log.Players
                .Where(t => t != null)
                .SelectMany(t => t)
                .Any(p => p.IsWinner);

            string[] teamLabels = { "RED TEAM", "BLUE TEAM" };

            for (int t = 0; t < log.Players.Count; t++)
            {
                List<UTPlayerMatchStats> team = log.Players[t];
                if (team == null || team.Count == 0) continue;

                string label  = t < teamLabels.Length ? teamLabels[t] : $"TEAM {t}";
                string result = team.Any(p => p.IsWinner) ? "WINNER"
                              : anyWinner                 ? "LOSER"
                              :                            "DRAW";

                sb.AppendLine(Divider);
                sb.AppendLine($"  {label}  [{result}]");
                sb.AppendLine(Divider);

                foreach (UTPlayerMatchStats player in team.OrderBy(p => p.Placement))
                {
                    List<MatchEvent>? playerEvents = null;
                    if (log.Timeline != null && player.Guid != null)
                    {
                        playerEvents = log.Timeline
                            .Where(e => e.ActorGuid == player.Guid || e.TargetGuid == player.Guid)
                            .OrderBy(e => e.GameTimeSeconds)
                            .ToList();
                    }

                    WritePlayer(player, log.GameMode, sb, profileNames, anyWinner, playerEvents);
                }
            }
        }

        private static void WritePlayer(
            UTPlayerMatchStats p,
            UT2004GameMode mode,
            StringBuilder sb,
            IReadOnlyDictionary<string, string>? profileNames,
            bool anyWinner,
            List<MatchEvent>? playerEvents = null)
        {
            int    mins    = p.TotalTimeSeconds / 60;
            int    secs    = p.TotalTimeSeconds % 60;
            string winLoss = p.IsWinner ? "WIN" : (anyWinner ? "LOSS" : "DRAW");
            string botTag  = p.IsBot ? "  [BOT]" : string.Empty;
            string name    = ResolvePlayerName(p.Guid, p.LastKnownName, profileNames);

            sb.AppendLine(ThinDivider);
            sb.AppendLine($"  [{p.Placement}] {name}  (GUID: {p.Guid ?? "N/A"})  [{winLoss}]{botTag}");
            sb.AppendLine($"      Time       : {mins}m {secs}s");
            sb.AppendLine($"      Score      : {p.Score}  |  Kills: {p.Kills}  |  Deaths: {p.Deaths}  |  Suicides: {p.Suicides}  |  Headshots: {p.Headshots}");
            sb.AppendLine($"      K/D        : {p.GetKillDeathRatio():F2}  |  Best Streak: {p.BestKillStreak}  |  Best Multi-Kill: {p.BestMultiKill}");
            sb.AppendLine($"      Accuracy   : {p.GetOverallAccuracy():F2}%");

            WritePlayerTimeline(p, sb, playerEvents);

            switch (mode)
            {
                case UT2004GameMode.TAM:
                    WriteTAMStats(p, sb);
                    break;
                case UT2004GameMode.iCTF:
                    WriteICTFStats(p, sb);
                    break;
                case UT2004GameMode.iBR:
                    WriteIBRStats(p, sb);
                    break;
            }

            WriteWeaponStats(p, sb);
            sb.AppendLine();
        }

        /// <summary>
        /// Writes per-player kill/death timestamps and notable event highlights
        /// extracted from the match timeline.
        /// </summary>
        private static void WritePlayerTimeline(UTPlayerMatchStats p, StringBuilder sb, List<MatchEvent>? playerEvents)
        {
            if (playerEvents == null || playerEvents.Count == 0 || p.Guid == null) return;

            List<string> killTimes = playerEvents
                .Where(e => e.EventType == "Kill" && e.ActorGuid == p.Guid)
                .OrderBy(e => e.GameTimeSeconds)
                .Select(e => FormatGameTime(e.GameTimeSeconds))
                .ToList();

            List<string> deathTimes = playerEvents
                .Where(e => (e.EventType == "Kill"    && e.TargetGuid == p.Guid) ||
                            (e.EventType == "Suicide" && e.ActorGuid  == p.Guid))
                .OrderBy(e => e.GameTimeSeconds)
                .Select(e => FormatGameTime(e.GameTimeSeconds))
                .ToList();

            List<string> highlights = playerEvents
                .Where(e => e.ActorGuid == p.Guid && e.EventType is
                    "FirstBlood" or "Spree" or "MultiKill" or "Overkill" or
                    "FlagCapture" or "FlagReturn" or "BombCapture" or "BombThrown" or "BombPickup" or "BombDrop" or "BombTaken")
                .OrderBy(e => e.GameTimeSeconds)
                .Select(e =>
                {
                    string detail = e.Detail != null ? $" ({e.Detail})" : string.Empty;
                    return $"[{FormatGameTime(e.GameTimeSeconds)}] {e.EventType}{detail}";
                })
                .ToList();

            if (killTimes.Count > 0)
                sb.AppendLine($"      Kill Times : {string.Join("  ", killTimes)}");

            if (deathTimes.Count > 0)
                sb.AppendLine($"      Death Times: {string.Join("  ", deathTimes)}");

            if (highlights.Count > 0)
                sb.AppendLine($"      Highlights : {string.Join("  |  ", highlights)}");
        }

        private static void WriteTAMStats(UTPlayerMatchStats p, StringBuilder sb)
        {
            sb.AppendLine($"      -- TAM --");
            sb.AppendLine($"      Dmg Dealt  : {p.TotalDamageDealt}  |  Dmg Taken: {p.TotalDamageTaken}  |  FF Dmg: {p.FriendlyFireDamage}");
            sb.AppendLine($"      Dmg/Round  : {p.GetDamagePerRound():F1}  |  Round-Ending Kills: {p.RoundEndingKills}");
            sb.AppendLine($"      Rounds Won : {p.RoundsWon}  |  Rounds Played: {p.RoundsPlayed}");
        }

        private static void WriteICTFStats(UTPlayerMatchStats p, StringBuilder sb)
        {
            sb.AppendLine($"      -- iCTF --");
            sb.AppendLine($"      Flag Caps  : {p.FlagCaptures}  |  Flag Grabs: {p.FlagGrabs}  |  Flag Pickups: {p.FlagPickups}");
            sb.AppendLine($"      Flag Drops : {p.FlagDrops}  |  Flag Returns: {p.FlagReturns}  |  Flag Denials: {p.FlagDenials}");
            sb.AppendLine($"      Cap Assists: {p.FlagCaptureAssists}  |  1st Touch Caps: {p.FlagCaptureFirstTouch}");
            sb.AppendLine($"      Team Protect Frags: {p.TeamProtectFrags}  |  Critical Frags: {p.CriticalFrags}");
        }

        private static void WriteIBRStats(UTPlayerMatchStats p, StringBuilder sb)
        {
            sb.AppendLine($"      -- iBR --");
            sb.AppendLine($"      Ball Caps  : {p.BallCaptures}  |  Score Assists: {p.BallScoreAssists}  |  Ball Thrown Finals: {p.BallThrownFinals}");
            sb.AppendLine($"      Bomb Picks : {p.BombPickups}  |  Bomb Drops: {p.BombDrops}  |  Bomb Taken: {p.BombTaken}");
            sb.AppendLine($"      Bomb Returned (Timeout): {p.BombReturnedTimeouts}");
        }

        private static void WriteWeaponStats(UTPlayerMatchStats p, StringBuilder sb)
        {
            if (p.WeaponStatistics == null || p.WeaponStatistics.Count == 0) return;

            sb.AppendLine($"      -- Weapons --");
            foreach (KeyValuePair<string, WeaponStats> kvp in p.WeaponStatistics.OrderByDescending(w => w.Value.DamageDealt))
            {
                WeaponStats w = kvp.Value;
                sb.AppendLine($"      {kvp.Key,-32} Shots: {w.ShotsFired,4}  |  Hits: {w.Hits,4}  |  Dmg: {w.DamageDealt,6}  |  Acc: {w.GetAccuracy():F2}%  |  Dmg/Hit: {w.GetDamagePerHit():F1}");
            }
        }

        private static void WriteKillMatrix(UT2004StatLog log, StringBuilder sb, IReadOnlyDictionary<string, string>? profileNames)
        {
            if (log.KillMatch == null || log.KillMatch.Count == 0) return;

            Dictionary<string, string> lookup = BuildGuidNameLookup(log, profileNames);

            sb.AppendLine(Divider);
            sb.AppendLine("  KILL MATRIX");
            sb.AppendLine(Divider);

            foreach (KeyValuePair<string, Dictionary<string, int>> killer in log.KillMatch.OrderByDescending(k => k.Value.Values.Sum()))
            {
                string killerName = lookup.TryGetValue(killer.Key, out string? kn) ? kn : killer.Key;
                int    totalKills = killer.Value.Values.Sum();

                sb.AppendLine($"  {killerName}  (Total Kills: {totalKills})");

                foreach (KeyValuePair<string, int> victim in killer.Value.OrderByDescending(v => v.Value))
                {
                    string victimName = lookup.TryGetValue(victim.Key, out string? vn) ? vn : victim.Key;
                    sb.AppendLine($"    -> {victimName}: {victim.Value}x");
                }
            }

            sb.AppendLine();
        }

        /// <summary>
        /// Writes a full chronological event listing for the match, starting from game start (t ≥ 0).
        /// Null actor/target names in stored events are resolved via the GUID lookup so that matches
        /// with a missing <c>LastKnownName</c> in the JSON still display correctly.
        /// </summary>
        private static void WriteTimeline(UT2004StatLog log, StringBuilder sb, IReadOnlyDictionary<string, string>? profileNames = null)
        {
            if (log.Timeline == null || log.Timeline.Count == 0) return;

            List<MatchEvent> events = log.Timeline
                .Where(e => e.GameTimeSeconds >= 0)
                .OrderBy(e => e.GameTimeSeconds)
                .ToList();

            if (events.Count == 0) return;

            // Build lookup once so null-named events (e.g. stored before profileNames existed)
            // still resolve to a readable name via the GUID.
            Dictionary<string, string> lookup = BuildGuidNameLookup(log, profileNames);

            sb.AppendLine(Divider);
            sb.AppendLine($"  MATCH TIMELINE  ({events.Count} events)");
            sb.AppendLine(Divider);

            foreach (MatchEvent e in events)
            {
                string time      = FormatGameTime(e.GameTimeSeconds);
                string eventType = e.EventType.PadRight(18);

                bool   hasTarget   = !string.IsNullOrEmpty(e.TargetGuid) || !string.IsNullOrEmpty(e.TargetName);
                string actorDisplay = ResolveEventName(e.ActorName, e.ActorGuid, lookup);
                string body;

                if (hasTarget)
                {
                    string targetDisplay = ResolveEventName(e.TargetName, e.TargetGuid, lookup, "?");
                    body = $"{(string.IsNullOrEmpty(actorDisplay) ? "?" : actorDisplay)} → {targetDisplay}";
                }
                else
                {
                    body = actorDisplay;
                }

                string detail = !string.IsNullOrEmpty(e.Detail) ? $"  ({e.Detail})" : string.Empty;

                sb.AppendLine($"  [{time}] {eventType} {body}{detail}");
            }

            sb.AppendLine();
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Formats a game-relative timestamp as <c>MM:SS</c>.
        /// Negative values (pre-game) are formatted as <c>-MM:SS</c>.
        /// </summary>
        private static string FormatGameTime(double seconds)
        {
            bool   negative = seconds < 0;
            double abs      = Math.Abs(seconds);
            int    mins     = (int)(abs / 60);
            int    secs     = (int)(abs % 60);
            return negative ? $"-{mins:D2}:{secs:D2}" : $"{mins:D2}:{secs:D2}";
        }

        /// <summary>
        /// Resolves a display name for a <see cref="MatchEvent"/> actor or target.
        /// Prefers the stored name, then the GUID lookup, then <paramref name="fallback"/>.
        /// </summary>
        private static string ResolveEventName(string? name, string? guid, Dictionary<string, string> lookup, string fallback = "")
        {
            if (!string.IsNullOrEmpty(name)) return name;
            if (guid != null && lookup.TryGetValue(guid, out string? resolved) && !string.IsNullOrEmpty(resolved))
                return resolved;
            return fallback;
        }

        /// <summary>
        /// Resolves a display name for a player, falling back to the profile lookup then a
        /// hard "Unknown" sentinel if no name is available at all.
        /// </summary>
        private static string ResolvePlayerName(string? guid, string? lastKnownName, IReadOnlyDictionary<string, string>? profileNames)
        {
            if (!string.IsNullOrWhiteSpace(lastKnownName))
                return lastKnownName;

            if (guid != null && profileNames != null && profileNames.TryGetValue(guid, out string? profileName) && !string.IsNullOrWhiteSpace(profileName))
                return profileName;

            return "Unknown";
        }

        private static Dictionary<string, string> BuildGuidNameLookup(UT2004StatLog log, IReadOnlyDictionary<string, string>? profileNames)
        {
            var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (log.Players == null) return lookup;

            foreach (List<UTPlayerMatchStats> team in log.Players)
            {
                if (team == null) continue;
                foreach (UTPlayerMatchStats player in team)
                {
                    if (player.Guid != null && !lookup.ContainsKey(player.Guid))
                        lookup[player.Guid] = ResolvePlayerName(player.Guid, player.LastKnownName, profileNames);
                }
            }

            return lookup;
        }
    }
}
