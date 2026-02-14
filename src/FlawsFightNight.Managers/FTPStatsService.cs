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
        private readonly DiscordSocketClient _discordClient;
        private AsyncFtpClient _ftpClient;

        public FTPStatsService(ConfigManager configManager, DiscordSocketClient client)
        {
            _configManager = configManager;
            _discordClient = client;
            ConfigureFTPClients();
        }

        private void ConfigureFTPClients()
        {
            // TODO Implement pulling creds from ConfigManager once they are finally being saved
            //var creds = _configManager.GetFTPCredentials();
            //_ftpClient = new(host: creds.Host, user: creds.Username, pass: creds.Password, port: creds.Port);
            _ftpClient = new(host: "127.0.0.1", user: "bot_test", pass: "password1", port: 21);

            // Configure TLS/SSL settings BEFORE connecting
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
                    await GetFileCountFromDirectoryLocation("/placeholderDir/anotherDir/UserLogs");

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{DateTime.Now} - [FTPStatsService] Error: {ex}");
                }

                await Task.Delay(TimeSpan.FromSeconds(9999), token);
            }
        }

        private async Task GetFileCountFromDirectoryLocation(string directory)
        {
            var items = await _ftpClient.GetListing(directory);

            foreach (var item in items)
            {
                switch (item.Type)
                {
                    case FtpObjectType.Directory:
                        Console.WriteLine($"{item.Name} is a directory.");
                        break;
                    case FtpObjectType.File:
                        Console.WriteLine($"{item.Name} - {item.GetHashCode()}");
                        break;
                    default:
                        Console.WriteLine($"{item.Name} is of unknown type.");
                        break;
                }
            }
            // Print total count of files in the directory
            int fileCount = items.Count();
            Console.WriteLine($"Total files in directory '{directory}': {fileCount}");
        }
    }
}
