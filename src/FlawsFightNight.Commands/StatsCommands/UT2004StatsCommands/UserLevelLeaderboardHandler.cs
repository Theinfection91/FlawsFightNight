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

        public UserLevelLeaderboardHandler(EmbedFactory embedFactory, UT2004StatsService ut2004StatsService) : base("User Level Leaderboard")
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

            return (BuildLeaderboardEmbed(profiles, "general"), true);
        }

        public Embed HandleSection(string section)
        {
            var profiles = _ut2004StatsService.GetAllPrimaryPlayerProfiles();
            if (profiles == null || profiles.Count == 0)
                return _embedFactory.ErrorEmbed(Name, "No UT2004 player profiles found.");

            return BuildLeaderboardEmbed(profiles, section);
        }

        private Embed BuildLeaderboardEmbed(List<UT2004PlayerProfile> profiles, string section)
        {
            return section switch
            {
                "ictf" => BuildCTFLeaderboard(profiles),
                "tam" => BuildTAMLeaderboard(profiles),
                "ibr" => BuildBRLeaderboard(profiles),
                _ => BuildGeneralLeaderboard(profiles)
            };
        }

        private Embed BuildGeneralLeaderboard(List<UT2004PlayerProfile> profiles)
        {
            var sorted = profiles
                .OrderByDescending(p => p.TotalMatches)
                .Take(15)
                .ToList();

            var sb = new StringBuilder();
            for (int i = 0; i < sorted.Count; i++)
            {
                var p = sorted[i];
                string medal = i switch { 0 => "🥇", 1 => "🥈", 2 => "🥉", _ => $"**{i + 1}.**" };
                sb.AppendLine($"{medal} **{p.CurrentName}** — {p.TotalMatches} matches · {p.Wins}W/{p.Losses}L · K/D: {p.KDRatio:F2} · WR: {p.WinRate:P0}");
            }

            var embed = new EmbedBuilder()
                .WithTitle("📊 UT2004 Leaderboard — General")
                .WithDescription(sb.ToString())
                .WithColor(new Color(0xFF6A00))
                .WithFooter("Flaws Fight Night — UT2004 Leaderboard · General")
                .WithCurrentTimestamp();

            return embed.Build();
        }

        private Embed BuildCTFLeaderboard(List<UT2004PlayerProfile> profiles)
        {
            var sorted = profiles
                .Where(p => p.TotalCTFMatches > 0)
                .OrderByDescending(p => p.CaptureTheFlagElo.Rating)
                .Take(15)
                .ToList();

            var sb = new StringBuilder();
            if (sorted.Count == 0)
            {
                sb.AppendLine("_No iCTF matches played yet._");
            }
            else
            {
                for (int i = 0; i < sorted.Count; i++)
                {
                    var p = sorted[i];
                    string medal = i switch { 0 => "🥇", 1 => "🥈", 2 => "🥉", _ => $"**{i + 1}.**" };
                    sb.AppendLine($"{medal} **{p.CurrentName}** — ELO: {p.CaptureTheFlagElo.Rating:F1} · {p.TotalCTFMatches} matches · WR: {p.CTFWinRate:P0} · Caps: {p.TotalFlagCaptures}");
                }
            }

            var embed = new EmbedBuilder()
                .WithTitle("🚩 UT2004 Leaderboard — iCTF")
                .WithDescription(sb.ToString())
                .WithColor(new Color(0xFF6A00))
                .WithFooter("Flaws Fight Night — UT2004 Leaderboard · iCTF")
                .WithCurrentTimestamp();

            return embed.Build();
        }

        private Embed BuildTAMLeaderboard(List<UT2004PlayerProfile> profiles)
        {
            var sorted = profiles
                .Where(p => p.TotalTAMMatches > 0)
                .OrderByDescending(p => p.TAMElo.Rating)
                .Take(15)
                .ToList();

            var sb = new StringBuilder();
            if (sorted.Count == 0)
            {
                sb.AppendLine("_No TAM matches played yet._");
            }
            else
            {
                for (int i = 0; i < sorted.Count; i++)
                {
                    var p = sorted[i];
                    string medal = i switch { 0 => "🥇", 1 => "🥈", 2 => "🥉", _ => $"**{i + 1}.**" };
                    sb.AppendLine($"{medal} **{p.CurrentName}** — ELO: {p.TAMElo.Rating:F1} · {p.TotalTAMMatches} matches · WR: {p.TAMWinRate:P0} · Avg Dmg: {p.AverageDamagePerMatch:F0}");
                }
            }

            var embed = new EmbedBuilder()
                .WithTitle("🎯 UT2004 Leaderboard — TAM")
                .WithDescription(sb.ToString())
                .WithColor(new Color(0xFF6A00))
                .WithFooter("Flaws Fight Night — UT2004 Leaderboard · TAM")
                .WithCurrentTimestamp();

            return embed.Build();
        }

        private Embed BuildBRLeaderboard(List<UT2004PlayerProfile> profiles)
        {
            var sorted = profiles
                .Where(p => p.TotalBRMatches > 0)
                .OrderByDescending(p => p.BombingRunElo.Rating)
                .Take(15)
                .ToList();

            var sb = new StringBuilder();
            if (sorted.Count == 0)
            {
                sb.AppendLine("_No iBR matches played yet._");
            }
            else
            {
                for (int i = 0; i < sorted.Count; i++)
                {
                    var p = sorted[i];
                    string medal = i switch { 0 => "🥇", 1 => "🥈", 2 => "🥉", _ => $"**{i + 1}.**" };
                    sb.AppendLine($"{medal} **{p.CurrentName}** — ELO: {p.BombingRunElo.Rating:F1} · {p.TotalBRMatches} matches · WR: {p.BRWinRate:P0} · Ball Caps: {p.TotalBallCaptures}");
                }
            }

            var embed = new EmbedBuilder()
                .WithTitle("💣 UT2004 Leaderboard — iBR")
                .WithDescription(sb.ToString())
                .WithColor(new Color(0xFF6A00))
                .WithFooter("Flaws Fight Night — UT2004 Leaderboard · iBR")
                .WithCurrentTimestamp();

            return embed.Build();
        }
    }
}
