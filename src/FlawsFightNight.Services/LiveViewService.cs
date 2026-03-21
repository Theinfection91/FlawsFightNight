using Discord;
using Discord.WebSocket;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Models.Stats;
using FlawsFightNight.Core.Models.Tournaments;
using Microsoft.Extensions.Hosting;
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

        public LiveViewService(
            DiscordSocketClient client,
            EmbedFactory embedFactory,
            GitBackupService gitBackupService,
            DataContext dataContext)
        {
            _client = client;
            _embedFactory = embedFactory;
            _gitBackupService = gitBackupService;
            _dataContext = dataContext;
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
                        switch (leaderboardChannel.Type)
                        {
                            case LeaderboardChannelTypes.GeneralUT2004:
                                await UpdateUT2004GeneralLeaderboard(leaderboardChannel, token);
                                break;
                            case LeaderboardChannelTypes.iBR:
                                await UpdateUT2004BRLeaderboard(leaderboardChannel, token);
                                break;
                            case LeaderboardChannelTypes.iCTF:
                                await UpdateUT2004CTFLeaderboard(leaderboardChannel, token);
                                break;
                            case LeaderboardChannelTypes.TAM:
                                await UpdateUT2004TAMLeaderboard(leaderboardChannel, token);
                                break;
                            default:
                                Console.WriteLine($"{DateTime.Now} - [LiveViewService] Unknown leaderboard channel type: {leaderboardChannel.Type}");
                                break;
                        }
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

        private async Task UpdateUT2004GeneralLeaderboard(LeaderboardChannelData leaderboardChannel, CancellationToken token)
        {
            if (leaderboardChannel == null) return;
            if (leaderboardChannel.ChannelId == 0) return;
            var channel = _client.GetChannel(leaderboardChannel.ChannelId) as IMessageChannel;
            if (channel == null) return;
            Embed embed = null; // TODO: Create embed factory method for this, maybe components too?
            if (leaderboardChannel.MessageId != 0)
            {
                var existing = await channel.GetMessageAsync(leaderboardChannel.MessageId) as IUserMessage;
                if (existing != null)
                {
                    await existing.ModifyAsync(m => m.Embed = embed);
                    return;
                }
            }
            var newMsg = await channel.SendMessageAsync(embed: embed);
            leaderboardChannel.MessageId = newMsg.Id;

            await _dataContext.SaveAndReloadLeaderboardChannelsFile();
            _gitBackupService.EnqueueBackup();
        }

        private async Task UpdateUT2004BRLeaderboard(LeaderboardChannelData leaderboardChannel, CancellationToken token)
        {
            if (leaderboardChannel == null) return;
            if (leaderboardChannel.ChannelId == 0) return;
            var channel = _client.GetChannel(leaderboardChannel.ChannelId) as IMessageChannel;
            if (channel == null) return;
            Embed embed = null; // TODO: Create embed factory method for BR leaderboard
            if (leaderboardChannel.MessageId != 0)
            {
                var existing = await channel.GetMessageAsync(leaderboardChannel.MessageId) as IUserMessage;
                if (existing != null)
                {
                    await existing.ModifyAsync(m => m.Embed = embed);
                    return;
                }
            }
            var newMsg = await channel.SendMessageAsync(embed: embed);
            leaderboardChannel.MessageId = newMsg.Id;

            await _dataContext.SaveAndReloadLeaderboardChannelsFile();
            _gitBackupService.EnqueueBackup();
        }

        private async Task UpdateUT2004CTFLeaderboard(LeaderboardChannelData leaderboardChannel, CancellationToken token)
        {
            if (leaderboardChannel == null) return;
            if (leaderboardChannel.ChannelId == 0) return;
            var channel = _client.GetChannel(leaderboardChannel.ChannelId) as IMessageChannel;
            if (channel == null) return;
            Embed embed = null; // TODO: Create embed factory method for CTF leaderboard
            if (leaderboardChannel.MessageId != 0)
            {
                var existing = await channel.GetMessageAsync(leaderboardChannel.MessageId) as IUserMessage;
                if (existing != null)
                {
                    await existing.ModifyAsync(m => m.Embed = embed);
                    return;
                }
            }
            var newMsg = await channel.SendMessageAsync(embed: embed);
            leaderboardChannel.MessageId = newMsg.Id;

            await _dataContext.SaveAndReloadLeaderboardChannelsFile();
            _gitBackupService.EnqueueBackup();
        }

        private async Task UpdateUT2004TAMLeaderboard(LeaderboardChannelData leaderboardChannel, CancellationToken token)
        {
            if (leaderboardChannel == null) return;
            if (leaderboardChannel.ChannelId == 0) return;
            var channel = _client.GetChannel(leaderboardChannel.ChannelId) as IMessageChannel;
            if (channel == null) return;
            Embed embed = null; // TODO: Create embed factory method for TAM leaderboard
            if (leaderboardChannel.MessageId != 0)
            {
                var existing = await channel.GetMessageAsync(leaderboardChannel.MessageId) as IUserMessage;
                if (existing != null)
                {
                    await existing.ModifyAsync(m => m.Embed = embed);
                    return;
                }
            }
            var newMsg = await channel.SendMessageAsync(embed: embed);
            leaderboardChannel.MessageId = newMsg.Id;

            await _dataContext.SaveAndReloadLeaderboardChannelsFile();
            _gitBackupService.EnqueueBackup();
        }
    }
}
