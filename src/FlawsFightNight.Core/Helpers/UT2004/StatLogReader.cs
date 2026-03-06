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
        private const string Divider    = "================================================";
        private const string ThinDivider = "------------------------------------------------";

        /// <summary>
        /// Returns a formatted, human-readable string of a <see cref="UT2004StatLog"/>.
        /// </summary>
        public static string ReadStatLog(UT2004StatLog log)
        {
            var sb = new StringBuilder();

            WriteHeader(log, sb);
            WriteTeams(log, sb);
            WriteKillMatrix(log, sb);

            return sb.ToString();
        }

        /// <summary>
        /// Returns a UTF-8 encoded <see cref="MemoryStream"/> of the formatted stat log,
        /// ready to be sent as a .txt file attachment.
        /// </summary>
        public static MemoryStream ReadStatLogAsStream(UT2004StatLog log)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(ReadStatLog(log));
            return new MemoryStream(bytes) { Position = 0 };
        }

        // ── Section writers ──────────────────────────────────────────────────────

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

        private static void WriteTeams(UT2004StatLog log, StringBuilder sb)
        {
            if (log.Players == null || log.Players.Count == 0)
            {
                sb.AppendLine("  No player data available.");
                return;
            }

            string[] teamLabels = { "RED TEAM", "BLUE TEAM" };

            for (int t = 0; t < log.Players.Count; t++)
            {
                List<UTPlayerMatchStats> team = log.Players[t];
                if (team == null || team.Count == 0) continue;

                string label  = t < teamLabels.Length ? teamLabels[t] : $"TEAM {t}";
                string result = team.Any(p => p.IsWinner) ? "WINNER" : "LOSER";

                sb.AppendLine(Divider);
                sb.AppendLine($"  {label}  [{result}]");
                sb.AppendLine(Divider);

                foreach (UTPlayerMatchStats player in team.OrderBy(p => p.Placement))
                {
                    WritePlayer(player, log.GameMode, sb);
                }
            }
        }

        private static void WritePlayer(UTPlayerMatchStats p, UT2004GameMode mode, StringBuilder sb)
        {
            int    mins    = p.TotalTimeSeconds / 60;
            int    secs    = p.TotalTimeSeconds % 60;
            string winLoss = p.IsWinner ? "WIN" : "LOSS";
            string botTag  = p.IsBot ? "  [BOT]" : string.Empty;

            sb.AppendLine(ThinDivider);
            sb.AppendLine($"  [{p.Placement}] {p.LastKnownName ?? "Unknown"}  (GUID: {p.Guid ?? "N/A"})  [{winLoss}]{botTag}");
            sb.AppendLine($"      Time       : {mins}m {secs}s");
            sb.AppendLine($"      Score      : {p.Score}  |  Kills: {p.Kills}  |  Deaths: {p.Deaths}  |  Suicides: {p.Suicides}  |  Headshots: {p.Headshots}");
            sb.AppendLine($"      K/D        : {p.GetKillDeathRatio():F2}  |  Best Streak: {p.BestKillStreak}  |  Best Multi-Kill: {p.BestMultiKill}");
            sb.AppendLine($"      Accuracy   : {p.GetOverallAccuracy():F2}%");

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

        private static void WriteKillMatrix(UT2004StatLog log, StringBuilder sb)
        {
            if (log.KillMatch == null || log.KillMatch.Count == 0) return;

            Dictionary<string, string> lookup = BuildGuidNameLookup(log);

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

        // ── Helpers ──────────────────────────────────────────────────────────────

        private static Dictionary<string, string> BuildGuidNameLookup(UT2004StatLog log)
        {
            var lookup = new Dictionary<string, string>();
            if (log.Players == null) return lookup;

            foreach (List<UTPlayerMatchStats> team in log.Players)
            {
                if (team == null) continue;
                foreach (UTPlayerMatchStats player in team)
                {
                    if (player.Guid != null && !lookup.ContainsKey(player.Guid))
                        lookup[player.Guid] = player.LastKnownName ?? player.Guid;
                }
            }

            return lookup;
        }
    }
}
