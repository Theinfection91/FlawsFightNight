using Discord;
using Discord.WebSocket;
using FluentFTP;
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
        private readonly ConfigManager _configManager;
        private readonly UT2004StatsManager _ut2004StatsManager;

        private readonly DiscordSocketClient _discordClient;
        private AsyncFtpClient? _ftpClient;

        public FTPStatsService(ConfigManager configManager, DiscordSocketClient client, UT2004StatsManager uT2004StatsManager)
        {
            _configManager = configManager;
            _ut2004StatsManager = uT2004StatsManager;

            _discordClient = client;
            ConfigureFTPClients();
        }

        private void ConfigureFTPClients()
        {
            // TODO Implement pulling creds from ConfigManager once they are finally being saved
            //var creds = _configManager.GetFTPCredentials();
            //_ftpClient = new(host: creds.Host, user: creds.Username, pass: creds.Password, port: creds.Port);
            _ftpClient = new(host: "127.0.0.1", user: "bot_test", pass: "password1", port: 21);

            // Configure TLS/SSL settings
            _ftpClient.Config.EncryptionMode = FtpEncryptionMode.Explicit; // or FtpEncryptionMode.Auto
            _ftpClient.Config.ValidateAnyCertificate = true; // Accept self-signed certificates (for local dev)
            _ftpClient.Config.SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13;

            // Configure listing parser
            _ftpClient.Config.ListingParser = FtpParser.Machine;

            _ftpClient.AutoConnect();
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
            _discordClient.Ready += ReadyHandler;
            await tcs.Task;
            _discordClient.Ready -= ReadyHandler;

            Console.WriteLine($"{DateTime.Now} - [FTPStatsService] Starting service...");

            while (!token.IsCancellationRequested)
            {
                try
                {

                    //Console.WriteLine($"Connected = {_ftpClient.IsConnected}");
                    //Console.WriteLine($"{DateTime.Now} [FTPStatsService] Heartbeat...");

                    // Direct connect for now for testing - eventually will want to pull creds from ConfigManager and handle connection issues/retries more robustly
                    string magicDirectory = "/placeholderDir/anotherDir/UserLogs";
                    if (await ContainsFreshLogs(magicDirectory))
                    {
                        Console.WriteLine($"{DateTime.Now} - [FTPStatsService] Fresh logs found! Processing...");

                        var items = await _ftpClient.GetListing(magicDirectory);
                        foreach (var item in items)
                        {
                            if (item.Name.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
                            {
                                if (await _ut2004StatsManager.IsLogFileProcessed(item.Name))
                                {
                                    //Console.WriteLine($"{DateTime.Now} - [FTPStatsService] Skipping already processed log: {item.Name}");
                                    continue;
                                }
                                else
                                {
                                    Console.WriteLine($"{DateTime.Now} - [FTPStatsService] Processing new log: {item.Name}");
                                    var fileStream = await _ftpClient.OpenRead(item.FullName);
                                    string fileName = item.Name;
                                    await _ut2004StatsManager.ProcessLogFile(fileStream, fileName);
                                }
                            }
                        }
                        var allStats = await _ut2004StatsManager.GetAllProcessedStatLogs();
                        Console.WriteLine($"{DateTime.Now} - [FTPStatsService] Total processed stat logs: {allStats.Count}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{DateTime.Now} - [FTPStatsService] Error: {ex}");
                }

                await Task.Delay(TimeSpan.FromSeconds(9999), token);
            }
        }     

        private async Task<bool> ContainsFreshLogs(string directory)
        {
            var items = await _ftpClient.GetListing(directory);

            foreach (var item in items)
            {
                if (item.Type == FtpObjectType.File)
                {
                    if (item.Name.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!await _ut2004StatsManager.IsLogFileProcessed(item.Name))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
