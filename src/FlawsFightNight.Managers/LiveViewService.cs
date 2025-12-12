using Discord;
using Discord.WebSocket;
using FlawsFightNight.Core.Models.Tournaments;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Managers
{
    public class LiveViewService : BackgroundService
    {
        private readonly DiscordSocketClient _client;
        private readonly EmbedManager _embedManager;
        private readonly GitBackupManager _gitBackupManager;
        private readonly DataManager _dataManager;

        public LiveViewService(
            DiscordSocketClient client,
            EmbedManager embedManager,
            GitBackupManager gitBackupManager,
            DataManager dataManager)
        {
            _client = client;
            _embedManager = embedManager;
            _gitBackupManager = gitBackupManager;
            _dataManager = dataManager;
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

            Console.WriteLine($"{DateTime.Now} - [LiveViewService] Starting service...");

            while (!token.IsCancellationRequested)
            {
                try
                {
                    //Console.WriteLine($"{DateTime.Now} [LiveViewService] Heartbeat...");

                    var tournaments = _dataManager.GetTournaments()
                        .Where(t => t != null)
                        .ToList();

                    foreach (var t in tournaments)
                    {
                        await UpdateMatchesAsync(t, token);
                        await UpdateStandingsAsync(t, token);
                        await UpdateTeamsAsync(t, token);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{DateTime.Now} - [LiveViewService] Exception: {ex}");
                }

                await Task.Delay(TimeSpan.FromSeconds(15), token);
            }
        }

        private async Task UpdateMatchesAsync(Tournament tournament, CancellationToken token)
        {
            if (tournament.MatchesChannelId == 0) return;

            var channel = _client.GetChannel(tournament.MatchesChannelId) as IMessageChannel;
            if (channel == null) return;

            var embed = _embedManager.MatchesLiveViewResolver(tournament);

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
            await Task.Run(() =>
            {
                _dataManager.SaveAndReloadTournamentDataFiles(tournament);
                _gitBackupManager.CopyAndBackupFilesToGit();
            });
        }

        private async Task UpdateStandingsAsync(Tournament tournament, CancellationToken token)
        {
            if (tournament.StandingsChannelId == 0) return;
            var channel = _client.GetChannel(tournament.StandingsChannelId) as IMessageChannel;
            if (channel == null) return;
            var embed = _embedManager.StandingsLiveViewResolver(tournament);
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
            await Task.Run(() =>
            {
                _dataManager.SaveAndReloadTournamentDataFiles(tournament);
                _gitBackupManager.CopyAndBackupFilesToGit();
            });
        }

        private async Task UpdateTeamsAsync(Tournament tournament, CancellationToken token)
        {
            if (tournament.TeamsChannelId == 0) return;
            var channel = _client.GetChannel(tournament.TeamsChannelId) as IMessageChannel;
            if (channel == null) return;
            var embed = _embedManager.TeamsLiveView(tournament);
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
            await Task.Run(() =>
            {
                _dataManager.SaveAndReloadTournamentDataFiles(tournament);
                _gitBackupManager.CopyAndBackupFilesToGit();
            });
        }
    }
}
