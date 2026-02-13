using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Managers
{
    public class FTPStatsService : BackgroundService
    {
        private readonly DiscordSocketClient _client;

        public FTPStatsService(DiscordSocketClient client)
        {
            _client = client;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
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

            Console.WriteLine($"{DateTime.Now} - [FTPStatsService] Starting service...");

            while (!token.IsCancellationRequested)
            {
                try
                {
                    Console.WriteLine($"{DateTime.Now} [FTPStatsService] Heartbeat...");
                    await Task.Delay(TimeSpan.FromSeconds(60), token);
                }
                catch (TaskCanceledException)
                {
                    // Expected when the service is stopping, no action needed
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{DateTime.Now} - [FTPStatsService] Error: {ex}");
                }
            }
        }
    }
}
