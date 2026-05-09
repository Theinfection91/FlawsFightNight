using Discord;
using Discord.WebSocket;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Models.Stats;
using FlawsFightNight.Core.Models.Tournaments;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Services
{
    public class LiveViewService : BackgroundService
    {
        private readonly DiscordSocketClient _client;
        private readonly EmbedFactory _embedFactory;
        private readonly GitBackupService _gitBackupService;
        private readonly DataContext _dataContext;
        private readonly UT2004StatsService _ut2004StatsService;
        private readonly ILogger<LiveViewService> _logger;

        /// <summary>
        /// Tracks how many consecutive update cycles a registered leaderboard channel
        /// was not found in the Discord client's cache. After <see cref="StaleChannelMissThreshold"/>
        /// consecutive misses the entry is treated as stale and automatically removed.
        /// </summary>
        private readonly Dictionary<ulong, int> _channelMissCount = new();
        private const int StaleChannelMissThreshold = 3;

        public LiveViewService(
            DiscordSocketClient client,
            EmbedFactory embedFactory,
            GitBackupService gitBackupService,
            DataContext dataContext,
            UT2004StatsService ut2004StatsService,
            ILogger<LiveViewService> logger)
        {
            _client = client;
            _embedFactory = embedFactory;
            _gitBackupService = gitBackupService;
            _dataContext = dataContext;
            _ut2004StatsService = ut2004StatsService;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Fire and forget
            _ = RunLoop(stoppingToken);
            return Task.CompletedTask;
        }

        private async Task RunLoop(CancellationToken token)
        {
            // Wait for Discord to be ready
            var tcs = new TaskCompletionSource();
            Task ReadyHandler()
            {
                tcs.TrySetResult();
                return Task.CompletedTask;
            }
            _client.Ready += ReadyHandler;

            await tcs.Task;
            _client.Ready -= ReadyHandler;

            _logger.LogInformation("Starting LiveView service...");

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var tournaments = _dataContext.GetTournaments();
                    tournaments = tournaments
                        .Where(t => t != null)
                        .ToList();

                    foreach (var t in tournaments)
                    {
                        await UpdateMatchesAsync(t, token);
                        await UpdateStandingsAsync(t, token);
                        await UpdateTeamsAsync(t, token);
                    }

                    var leaderboardChannels = _dataContext.GetAllLeaderboardChannels();
                    leaderboardChannels = leaderboardChannels
                        .Where(c => c != null)
                        .ToList();

                    foreach (var leaderboardChannel in leaderboardChannels)
                    {
                        await UpdateUT2004Leaderboard(leaderboardChannel, token);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "LiveView service exception.");
                }

                await Task.Delay(TimeSpan.FromSeconds(15), token);
            }
        }

        // ── Tournament LiveView helpers ───────────────────────────────────────

        private async Task UpdateMatchesAsync(Tournament tournament, CancellationToken token)
        {
            if (tournament.MatchesChannelId == 0) return;

            var channel = _client.GetChannel(tournament.MatchesChannelId) as IMessageChannel;
            if (channel == null) return;

            var embed = _embedFactory.MatchesLiveViewResolver(tournament);

            if (tournament.MatchesMessageId != 0)
            {
                var existing = await channel.GetMessageAsync(tournament.MatchesMessageId) as IUserMessage;
                if (existing != null)
                {
                    await existing.ModifyAsync(m => m.Embed = embed);
                    return;
                }
            }

            var newMsg = await channel.SendMessageAsync(embed: embed);
            tournament.MatchesMessageId = newMsg.Id;
            await Task.Run(async () =>
            {
                await _dataContext.SaveAndReloadTournamentDataFiles(tournament);
                _gitBackupService.EnqueueBackup();
            });
        }

        private async Task UpdateStandingsAsync(Tournament tournament, CancellationToken token)
        {
            if (tournament.StandingsChannelId == 0) return;
            var channel = _client.GetChannel(tournament.StandingsChannelId) as IMessageChannel;
            if (channel == null) return;
            var embed = _embedFactory.StandingsLiveViewResolver(tournament);
            if (tournament.StandingsMessageId != 0)
            {
                var existing = await channel.GetMessageAsync(tournament.StandingsMessageId) as IUserMessage;
                if (existing != null)
                {
                    await existing.ModifyAsync(m => m.Embed = embed);
                    return;
                }
            }
            var newMsg = await channel.SendMessageAsync(embed: embed);
            tournament.StandingsMessageId = newMsg.Id;
            await Task.Run(async () =>
            {
                await _dataContext.SaveAndReloadTournamentDataFiles(tournament);
                _gitBackupService.EnqueueBackup();
            });
        }

        private async Task UpdateTeamsAsync(Tournament tournament, CancellationToken token)
        {
            if (tournament.TeamsChannelId == 0) return;
            var channel = _client.GetChannel(tournament.TeamsChannelId) as IMessageChannel;
            if (channel == null) return;
            var embed = _embedFactory.TeamsLiveView(tournament);
            if (tournament.TeamsMessageId != 0)
            {
                var existing = await channel.GetMessageAsync(tournament.TeamsMessageId) as IUserMessage;
                if (existing != null)
                {
                    await existing.ModifyAsync(m => m.Embed = embed);
                    return;
                }
            }
            var newMsg = await channel.SendMessageAsync(embed: embed);
            tournament.TeamsMessageId = newMsg.Id;
            await Task.Run(async () =>
            {
                await _dataContext.SaveAndReloadTournamentDataFiles(tournament);
                _gitBackupService.EnqueueBackup();
            });
        }

        // ── UT2004 Leaderboard LiveView helpers ───────────────────────────────

        /// <summary>
        /// Builds the shared category select menu + "Request Full List (DM)" button
        /// attached to every leaderboard LiveView message.
        /// Mirrors ComponentFactory.CreateUT2004LeaderboardSelectMenu() without a Bot project dependency.
        /// </summary>
        private static MessageComponent BuildLeaderboardSelectMenu(string section = "general", bool withMenu = false)
        {
            var selectMenu = new SelectMenuBuilder()
                .WithCustomId("ut2004leaderboard_select")
                .WithPlaceholder("📊 Select a leaderboard category...")
                .AddOption("📊 General", "general", "Overall career stats")
                .AddOption("🚩 iCTF", "ictf", "Capture the Flag leaderboard")
                .AddOption("🎯 TAM", "tam", "Team Arena Master leaderboard")
                .AddOption("💣 iBR", "ibr", "Bombing Run leaderboard");

            string gameModeLabel = section switch
            {
                "ictf" => "iCTF",
                "tam" => "TAM",
                "ibr" => "iBR",
                _ => "General"
            };

            var builder = new ComponentBuilder();
            //.WithSelectMenu(selectMenu);

            if (withMenu is true)
            {
                builder.WithSelectMenu(selectMenu);
                
                return builder.Build();
            }

            builder = builder.WithButton($"📋 Request Full {gameModeLabel} Leaderboard", customId: $"ut2004leaderboard_all:{section}", style: ButtonStyle.Secondary);
            return builder.Build();
        }

        private static string GetLeaderboardSection(LeaderboardChannelTypes type) => type switch
        {
            LeaderboardChannelTypes.iCTF => "ictf",
            LeaderboardChannelTypes.TAM => "tam",
            LeaderboardChannelTypes.iBR => "ibr",
            _ => "general"
        };

        private async Task UpdateUT2004Leaderboard(LeaderboardChannelData leaderboardChannel, CancellationToken token)
        {
            if (leaderboardChannel == null) return;
            if (leaderboardChannel.ChannelId == 0) return;

            var channel = _client.GetChannel(leaderboardChannel.ChannelId) as IMessageChannel;
            if (channel == null)
            {
                _channelMissCount.TryGetValue(leaderboardChannel.ChannelId, out var misses);
                misses++;
                _channelMissCount[leaderboardChannel.ChannelId] = misses;

                _logger.LogWarning("Leaderboard channel {ChannelId} not found in cache (miss {Misses}/{Threshold}).", leaderboardChannel.ChannelId, misses, StaleChannelMissThreshold);

                if (misses >= StaleChannelMissThreshold)
                {
                    _logger.LogWarning("Leaderboard channel {ChannelId} missing for {Threshold} consecutive cycles. Removing stale entry.", leaderboardChannel.ChannelId, StaleChannelMissThreshold);
                    await _dataContext.RemoveLeaderboardChannel(leaderboardChannel.ChannelId);
                    _channelMissCount.Remove(leaderboardChannel.ChannelId);
                    _gitBackupService.EnqueueBackup();
                }
                return;
            }

            // Channel is reachable — clear any accumulated miss count
            _channelMissCount.Remove(leaderboardChannel.ChannelId);

            var profiles = _ut2004StatsService.GetAllPrimaryPlayerProfiles() ?? [];
            var section = GetLeaderboardSection(leaderboardChannel.Type);
            var embed = _embedFactory.UT2004LeaderboardEmbed(profiles, section);

            if (leaderboardChannel.MessageId != 0)
            {
                var existing = await channel.GetMessageAsync(leaderboardChannel.MessageId) as IUserMessage;
                if (existing != null)
                {
                    // Only update the embed — Discord preserves the dropdown component automatically.
                    await existing.ModifyAsync(m => m.Embed = embed);
                    return;
                }
            }

            var newMsg = await channel.SendMessageAsync(embed: embed, components: BuildLeaderboardSelectMenu(section, false));
            leaderboardChannel.MessageId = newMsg.Id;

            await _dataContext.SaveAndReloadLeaderboardChannelsFile();
            _gitBackupService.EnqueueBackup();
        }
    }
}
