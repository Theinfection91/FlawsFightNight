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
        private readonly SemaphoreSlim _semaphore = new(1, 1);

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
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Wait for Discord client readiness
            //if (_client.LoginState != LoginState.LoggedIn)
            //{
            //    var tcs = new TaskCompletionSource<bool>();

            //    Task ReadyHandler()
            //    {
            //        tcs.TrySetResult(true);
            //        return Task.CompletedTask;
            //    }

            //    _client.Ready += ReadyHandler;

            //    // If client already ready, complete immediately
            //    if (_client.LoginState == LoginState.LoggedIn)
            //        tcs.TrySetResult(true);

            //    await tcs.Task;
            //    _client.Ready -= ReadyHandler;
            //}

            Console.WriteLine($"{DateTime.Now} [LiveViewService] Starting live view loop");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _semaphore.WaitAsync(stoppingToken);

                    Console.WriteLine($"{DateTime.Now} Heartbeat...");

                    // Take a snapshot to avoid modifying collection while iterating
                    var tournaments = _dataManager.TournamentsDatabaseFile.Tournaments
                        .Where(t => t != null)
                        .ToList();

                    foreach (var t in tournaments)
                    {
                        try
                        {
                            Console.WriteLine($"{DateTime.Now} [LiveViewService] Updating tournament {t.Id}");
                            await UpdateMatchesAsync(t, stoppingToken);
                            await UpdateStandingsAsync(t, stoppingToken);
                            await UpdateTeamsAsync(t, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"{DateTime.Now} [LiveViewService] Tournament {t.Id} failed: {ex}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{DateTime.Now} [LiveViewService] Exception: {ex}");
                }
                finally
                {
                    _semaphore.Release();
                }

                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
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
            //await Task.Run(() =>
            //{
            //    _dataManager.SaveAndReloadTournamentsDatabase();
            //    _gitBackupManager.CopyAndBackupFilesToGit();
            //});
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
            //await Task.Run(() =>
            //{
            //    _dataManager.SaveAndReloadTournamentsDatabase();
            //    _gitBackupManager.CopyAndBackupFilesToGit();
            //});
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
            //await Task.Run(() =>
            //{
            //    _dataManager.SaveAndReloadTournamentsDatabase();
            //    _gitBackupManager.CopyAndBackupFilesToGit();
            //});
        }
    }
}
