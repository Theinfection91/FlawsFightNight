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
    public class ComparePlayersHandler : CommandHandler
    {
        private readonly EmbedFactory _embedFactory;
        private readonly MemberService _memberService;

        public ComparePlayersHandler(EmbedFactory embedFactory, MemberService memberService) : base("Compare Players")
        {
            _embedFactory = embedFactory;
            _memberService = memberService;
        }

        public Task<Embed> Handle(IUser player1, IUser player2)
        {
            if (player1.Id == player2.Id)
                return Task.FromResult(_embedFactory.ErrorEmbed(Name, "You can't compare a player with themselves. Pick two different players!"));

            var member1 = _memberService.GetMemberProfile(player1.Id);
            var member2 = _memberService.GetMemberProfile(player2.Id);

            if (member1 == null || member1.RegisteredUT2004GUIDs.Count == 0)
                return Task.FromResult(_embedFactory.ErrorEmbed(Name, $"**{player1.Username}** does not have a registered UT2004 GUID."));
            if (member2 == null || member2.RegisteredUT2004GUIDs.Count == 0)
                return Task.FromResult(_embedFactory.ErrorEmbed(Name, $"**{player2.Username}** does not have a registered UT2004 GUID."));

            var profile1 = _memberService.GetUT2004PlayerProfile(member1.RegisteredUT2004GUIDs.First());
            var profile2 = _memberService.GetUT2004PlayerProfile(member2.RegisteredUT2004GUIDs.First());

            if (profile1 == null)
                return Task.FromResult(_embedFactory.ErrorEmbed(Name, $"No UT2004 stats found for **{player1.Username}**. Stats may not have been processed yet."));
            if (profile2 == null)
                return Task.FromResult(_embedFactory.ErrorEmbed(Name, $"No UT2004 stats found for **{player2.Username}**. Stats may not have been processed yet."));

            return Task.FromResult(BuildComparisonEmbed(profile1, profile2));
        }

        private Embed BuildComparisonEmbed(UT2004PlayerProfile p1, UT2004PlayerProfile p2)
        {
            var embed = new EmbedBuilder()
                .WithTitle($"⚔️ {p1.CurrentName} vs {p2.CurrentName}")
                .WithDescription($"Head-to-head stat comparison\n*Left = **{p1.CurrentName}** · Right = **{p2.CurrentName}***")
                .WithColor(new Color(0xFF6A00))
                .WithFooter("Flaws Fight Night — UT2004 Player Comparison")
                .WithCurrentTimestamp();

            embed.AddField("📊 Overall",
                $"**Matches:** {p1.TotalMatches} vs {p2.TotalMatches}\n" +
                $"**Record:** {p1.Wins}W/{p1.Losses}L vs {p2.Wins}W/{p2.Losses}L\n" +
                $"**Win Rate:** {p1.WinRate:P1} vs {p2.WinRate:P1}\n" +
                $"**K/D:** {p1.KDRatio:F2} vs {p2.KDRatio:F2}\n" +
                $"**Total Kills:** {p1.TotalKills:N0} vs {p2.TotalKills:N0}\n" +
                $"**Headshots:** {p1.TotalHeadshots:N0} vs {p2.TotalHeadshots:N0}\n" +
                $"**Best Kill Streak:** {p1.BestKillStreak} vs {p2.BestKillStreak}",
                false);

            if (p1.TotalCTFMatches > 0 || p2.TotalCTFMatches > 0)
            {
                embed.AddField("🚩 iCTF",
                    $"**ELO:** {p1.CaptureTheFlagElo.Rating:F1} vs {p2.CaptureTheFlagElo.Rating:F1}\n" +
                    $"**Matches:** {p1.TotalCTFMatches} vs {p2.TotalCTFMatches}\n" +
                    $"**Win Rate:** {p1.CTFWinRate:P1} vs {p2.CTFWinRate:P1}\n" +
                    $"**K/D:** {p1.CTFKDRatio:F2} vs {p2.CTFKDRatio:F2}\n" +
                    $"**Caps:** {p1.TotalFlagCaptures} vs {p2.TotalFlagCaptures}\n" +
                    $"**Returns:** {p1.TotalFlagReturns} vs {p2.TotalFlagReturns}",
                    false);
            }

            if (p1.TotalTAMMatches > 0 || p2.TotalTAMMatches > 0)
            {
                embed.AddField("🎯 TAM",
                    $"**ELO:** {p1.TAMElo.Rating:F1} vs {p2.TAMElo.Rating:F1}\n" +
                    $"**Matches:** {p1.TotalTAMMatches} vs {p2.TotalTAMMatches}\n" +
                    $"**Win Rate:** {p1.TAMWinRate:P1} vs {p2.TAMWinRate:P1}\n" +
                    $"**K/D:** {p1.TAMKDRatio:F2} vs {p2.TAMKDRatio:F2}\n" +
                    $"**Avg Dmg/Match:** {p1.AverageDamagePerMatch:F0} vs {p2.AverageDamagePerMatch:F0}\n" +
                    $"**Round Win Rate:** {p1.TAMRoundWinRate:P1} vs {p2.TAMRoundWinRate:P1}",
                    false);
            }

            if (p1.TotalBRMatches > 0 || p2.TotalBRMatches > 0)
            {
                embed.AddField("💣 iBR",
                    $"**ELO:** {p1.BombingRunElo.Rating:F1} vs {p2.BombingRunElo.Rating:F1}\n" +
                    $"**Matches:** {p1.TotalBRMatches} vs {p2.TotalBRMatches}\n" +
                    $"**Win Rate:** {p1.BRWinRate:P1} vs {p2.BRWinRate:P1}\n" +
                    $"**K/D:** {p1.BRKDRatio:F2} vs {p2.BRKDRatio:F2}\n" +
                    $"**Ball Caps:** {p1.TotalBallCaptures} vs {p2.TotalBallCaptures}\n" +
                    $"**Avg Caps/Match:** {p1.AverageBallCapsPerBRMatch:F2} vs {p2.AverageBallCapsPerBRMatch:F2}",
                    false);
            }

            return embed.Build();
        }
    }
}
