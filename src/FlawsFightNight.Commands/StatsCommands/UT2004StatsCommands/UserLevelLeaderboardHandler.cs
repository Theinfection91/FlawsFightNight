using Discord;
using FlawsFightNight.Core.Models.UT2004;
using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.StatsCommands.UT2004StatsCommands
{
    public class UserLevelLeaderboardHandler : CommandHandler
    {
        private readonly EmbedFactory _embedFactory;
        private readonly UT2004StatsService _ut2004StatsService;

        public UserLevelLeaderboardHandler(EmbedFactory embedFactory, UT2004StatsService ut2004StatsService)
            : base("User Level Leaderboard")
        {
            _embedFactory = embedFactory;
            _ut2004StatsService = ut2004StatsService;
        }

        public async Task<(Embed embed, bool hasProfiles)> Handle()
        {
            var profiles = _ut2004StatsService.GetAllPrimaryPlayerProfiles();
            if (profiles == null || profiles.Count == 0)
            {
                return (
                    _embedFactory.ErrorEmbed(Name, "No UT2004 player profiles found. Stats may not have been processed yet."),
                    false);
            }

            return (_embedFactory.UT2004GeneralLeaderboardEmbed(profiles), true);
        }

        public Embed HandleSection(string section)
        {
            var profiles = _ut2004StatsService.GetAllPrimaryPlayerProfiles();
            if (profiles == null || profiles.Count == 0)
                return _embedFactory.ErrorEmbed(Name, "No UT2004 player profiles found.");

            return _embedFactory.UT2004LeaderboardEmbed(profiles, section);
        }

        /// <summary>
        /// Returns the full (untruncated) leaderboard for the given section formatted as plain text,
        /// intended to be sent as a .txt file DM. Includes deep stats per player.
        /// </summary>
        public (string text, string fileName) HandleAllSectionAsText(string section)
        {
            var profiles = _ut2004StatsService.GetAllPrimaryPlayerProfiles();
            var sb = new StringBuilder();

            string sectionLabel = section switch
            {
                "ictf" => "iCTF",
                "tam" => "TAM",
                "ibr" => "iBR",
                _ => "General"
            };

            sb.AppendLine($"╔══════════════════════════════════════════════════════════════════╗");
            sb.AppendLine($"║  Flaws Fight Night — UT2004 Full Leaderboard ({sectionLabel})");
            sb.AppendLine($"║  Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine($"╚══════════════════════════════════════════════════════════════════╝");
            sb.AppendLine();

            if (profiles == null || profiles.Count == 0)
            {
                sb.AppendLine("No UT2004 player profiles found.");
            }
            else
            {
                switch (section)
                {
                    case "ictf": FormatFullCTFLeaderboard(sb, profiles); break;
                    case "tam":  FormatFullTAMLeaderboard(sb, profiles); break;
                    case "ibr":  FormatFullBRLeaderboard(sb, profiles); break;
                    default:     FormatFullGeneralLeaderboard(sb, profiles); break;
                }
            }

            string fileName = $"leaderboard_{sectionLabel.ToLower()}_{DateTime.UtcNow:yyyyMMdd}.txt";
            return (sb.ToString(), fileName);
        }

        #region Full Leaderboard Formatters

        private static void FormatFullGeneralLeaderboard(StringBuilder sb, List<UT2004PlayerProfile> profiles)
        {
            var sorted = profiles.OrderByDescending(p => p.TotalMatches).ToList();

            if (sorted.Count == 0) { sb.AppendLine("No player profiles found."); return; }

            sb.AppendLine($"Total Players: {sorted.Count}");
            sb.AppendLine(new string('═', 70));

            for (int i = 0; i < sorted.Count; i++)
            {
                var p = sorted[i];
                string rank = $"#{i + 1}";

                sb.AppendLine($"┌─ {rank} {p.CurrentName}");
                sb.AppendLine($"│  Record:         {p.Wins}W / {p.Losses}L ({p.WinRate:P1} win rate)");
                sb.AppendLine($"│  Matches:        {p.TotalMatches} total");
                sb.AppendLine($"│  Score:          {p.TotalScore:N0} total · {p.AverageScorePerMatch:F1} avg/match");
                sb.AppendLine($"│  Kills/Deaths:   {p.TotalKills:N0} / {p.TotalDeaths:N0} (K/D: {p.KDRatio:F2})");
                sb.AppendLine($"│  Headshots:      {p.TotalHeadshots:N0} · Suicides: {p.TotalSuicides:N0}");
                sb.AppendLine($"│  Career Bests:   Kill Streak: {p.BestKillStreak} · Multi-Kill: {p.BestMultiKill}");
                sb.AppendLine($"│                  Most Kills: {p.MostKillsInMatch} · Highest Score: {p.HighestScoreInMatch}");
                sb.AppendLine($"│  Active:         {FormatDate(p.FirstSeen)} — {FormatDate(p.LastPlayed)}");

                if (p.TotalCTFMatches > 0 || p.TotalTAMMatches > 0 || p.TotalBRMatches > 0)
                {
                    sb.AppendLine($"│  Mode Breakdown: iCTF: {p.TotalCTFMatches} · TAM: {p.TotalTAMMatches} · iBR: {p.TotalBRMatches}");
                }

                if (p.TotalWeaponKills.Count > 0)
                {
                    var topWeapons = p.TotalWeaponKills
                        .OrderByDescending(w => w.Value)
                        .Take(3)
                        .Select(w => $"{w.Key}: {w.Value}");
                    sb.AppendLine($"│  Top Weapons:    {string.Join(" · ", topWeapons)}");
                }

                sb.AppendLine($"└{'─', 0}{new string('─', 69)}");
            }
        }

        private static void FormatFullCTFLeaderboard(StringBuilder sb, List<UT2004PlayerProfile> profiles)
        {
            var sorted = profiles
                .Where(p => p.TotalCTFMatches > 0)
                .OrderByDescending(p => p.CaptureTheFlagElo.Rating)
                .ToList();

            if (sorted.Count == 0) { sb.AppendLine("No iCTF matches played yet."); return; }

            sb.AppendLine($"Total iCTF Players: {sorted.Count}");
            sb.AppendLine(new string('═', 70));

            for (int i = 0; i < sorted.Count; i++)
            {
                var p = sorted[i];
                string rank = $"#{i + 1}";

                sb.AppendLine($"┌─ {rank} {p.CurrentName}");
                sb.AppendLine($"│  ELO:            {p.CaptureTheFlagElo.Rating:F1}");
                sb.AppendLine($"│  Record:         {p.TotalCTFWins}W / {p.TotalCTFLosses}L ({p.CTFWinRate:P1} win rate)");
                sb.AppendLine($"│  Matches:        {p.TotalCTFMatches}");
                sb.AppendLine($"│  Score:          {p.TotalCTFScore:N0} total · {p.AverageScorePerCTFMatch:F1} avg/match");
                sb.AppendLine($"│  Kills/Deaths:   {p.TotalCTFKills:N0} / {p.TotalCTFDeaths:N0} (K/D: {p.CTFKDRatio:F2}) · {p.AverageKillsPerCTFMatch:F1} avg kills/match");
                sb.AppendLine($"│  Headshots:      {p.TotalCTFHeadshots:N0} · Suicides: {p.TotalCTFSuicides:N0}");
                sb.AppendLine($"│  Flag Caps:      {p.TotalFlagCaptures} total · {p.AverageCapturesPerMatch:F2} avg/match · Best: {p.MostFlagCapsInMatch}");
                sb.AppendLine($"│  Flag Grabs:     {p.TotalFlagGrabs} · Pickups: {p.TotalFlagPickups} · Drops: {p.TotalFlagDrops}");
                sb.AppendLine($"│  Flag Returns:   {p.TotalFlagReturns} total · Best: {p.MostFlagReturnsInMatch}");
                sb.AppendLine($"│  Flag Assists:   {p.TotalFlagCaptureAssists} · 1st Touch: {p.TotalFlagCaptureFirstTouch} · Denials: {p.TotalFlagDenials}");
                sb.AppendLine($"│  Team Plays:     Protect Frags: {p.TotalTeamProtectFrags} · Critical Frags: {p.TotalCriticalFrags}");
                sb.AppendLine($"└{new string('─', 69)}");
            }
        }

        private static void FormatFullTAMLeaderboard(StringBuilder sb, List<UT2004PlayerProfile> profiles)
        {
            var sorted = profiles
                .Where(p => p.TotalTAMMatches > 0)
                .OrderByDescending(p => p.TAMElo.Rating)
                .ToList();

            if (sorted.Count == 0) { sb.AppendLine("No TAM matches played yet."); return; }

            sb.AppendLine($"Total TAM Players: {sorted.Count}");
            sb.AppendLine(new string('═', 70));

            for (int i = 0; i < sorted.Count; i++)
            {
                var p = sorted[i];
                string rank = $"#{i + 1}";

                sb.AppendLine($"┌─ {rank} {p.CurrentName}");
                sb.AppendLine($"│  ELO:            {p.TAMElo.Rating:F1}");
                sb.AppendLine($"│  Record:         {p.TotalTAMWins}W / {p.TotalTAMLosses}L ({p.TAMWinRate:P1} win rate)");
                sb.AppendLine($"│  Matches:        {p.TotalTAMMatches}");
                sb.AppendLine($"│  Score:          {p.TotalTAMScore:N0} total · {p.AverageScorePerTAMMatch:F1} avg/match");
                sb.AppendLine($"│  Kills/Deaths:   {p.TotalTAMKills:N0} / {p.TotalTAMDeaths:N0} (K/D: {p.TAMKDRatio:F2}) · {p.AverageKillsPerTAMMatch:F1} avg kills/match");
                sb.AppendLine($"│  Headshots:      {p.TotalTAMHeadshots:N0} · Suicides: {p.TotalTAMSuicides:N0}");
                sb.AppendLine($"│  Damage Dealt:   {p.TotalDamageDealt:N0} total · {p.AverageDamagePerMatch:F0} avg/match · {p.AverageDamagePerRound:F0} avg/round");
                sb.AppendLine($"│  Rounds:         {p.TotalRoundsWon}W / {p.TotalRoundsPlayed} played ({p.TAMRoundWinRate:P1}) · {p.AverageRoundsWonPerMatch:F1} avg won/match");
                sb.AppendLine($"│  Round Enders:   {p.TotalRoundEndingKills} total · Best in match: {p.MostRoundEndingKillsInMatch}");
                sb.AppendLine($"│  Best Match:     Dmg: {p.MostDamageInMatch:N0} · Rounds Won: {p.MostRoundsWonInMatch}");

                if (p.OverallWeaponAccuracy > 0)
                {
                    sb.AppendLine($"│  Weapon Acc:     {p.OverallWeaponAccuracy:F1}%");
                }

                if (p.TotalWeaponStatistics.Count > 0)
                {
                    var topByDmg = p.TotalWeaponStatistics
                        .OrderByDescending(w => w.Value.DamageDealt)
                        .Take(3)
                        .Select(w =>
                        {
                            double acc = w.Value.ShotsFired > 0 ? (double)w.Value.Hits / w.Value.ShotsFired * 100 : 0;
                            return $"{w.Key}: {w.Value.DamageDealt:N0} dmg ({acc:F1}%)";
                        });
                    sb.AppendLine($"│  Top Weapons:    {string.Join(" · ", topByDmg)}");
                }

                sb.AppendLine($"└{new string('─', 69)}");
            }
        }

        private static void FormatFullBRLeaderboard(StringBuilder sb, List<UT2004PlayerProfile> profiles)
        {
            var sorted = profiles
                .Where(p => p.TotalBRMatches > 0)
                .OrderByDescending(p => p.BombingRunElo.Rating)
                .ToList();

            if (sorted.Count == 0) { sb.AppendLine("No iBR matches played yet."); return; }

            sb.AppendLine($"Total iBR Players: {sorted.Count}");
            sb.AppendLine(new string('═', 70));

            for (int i = 0; i < sorted.Count; i++)
            {
                var p = sorted[i];
                string rank = $"#{i + 1}";

                sb.AppendLine($"┌─ {rank} {p.CurrentName}");
                sb.AppendLine($"│  ELO:            {p.BombingRunElo.Rating:F1}");
                sb.AppendLine($"│  Record:         {p.TotalBRWins}W / {p.TotalBRLosses}L ({p.BRWinRate:P1} win rate)");
                sb.AppendLine($"│  Matches:        {p.TotalBRMatches}");
                sb.AppendLine($"│  Score:          {p.TotalBRScore:N0} total");
                sb.AppendLine($"│  Kills/Deaths:   {p.TotalBRKills:N0} / {p.TotalBRDeaths:N0} (K/D: {p.BRKDRatio:F2})");
                sb.AppendLine($"│  Headshots:      {p.TotalBRHeadshots:N0} · Suicides: {p.TotalBRSuicides:N0}");
                sb.AppendLine($"│  Ball Caps:      {p.TotalBallCaptures} total · {p.AverageBallCapsPerBRMatch:F2} avg/match · Best: {p.MostBallCapsInMatch}");
                sb.AppendLine($"│  Ball Assists:   {p.TotalBallScoreAssists} · Thrown Finals: {p.TotalBallThrownFinals}");
                sb.AppendLine($"│  Bomb Pickups:   {p.TotalBombPickups} total · {p.AverageBombPickupsPerBRMatch:F2} avg/match · Best: {p.MostBombPickupsInMatch}");
                sb.AppendLine($"│  Bomb Stats:     Drops: {p.TotalBombDrops} · Taken: {p.TotalBombTaken} (Best: {p.MostBombTakenInMatch}) · Timeouts: {p.TotalBombReturnedTimeouts}");
                sb.AppendLine($"└{new string('─', 69)}");
            }
        }

        #endregion

        private static string FormatDate(DateTime date) =>
            date == DateTime.MinValue ? "N/A" : date.ToString("yyyy-MM-dd");
    }
}
