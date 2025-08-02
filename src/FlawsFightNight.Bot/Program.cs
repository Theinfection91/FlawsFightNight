using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using FlawsFightNight.CommandsLogic.TournamentCommands;
using FlawsFightNight.Data.Handlers;
using FlawsFightNight.Managers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace FlawsFightNight.Bot
{
    public class Program
    {
        private IServiceProvider? _services;
        private DiscordSocketClient? _client;
        private CommandService? _commands;
        private InteractionService? _interactionService;

        //private ConfigManager _configManager;
        public ConfigManager _configManager;

        public static async Task Main(string[] args)
        {
            Program program = new();
            await program.RunAsync();
        }

        public async Task RunAsync()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info,
                GatewayIntents =
                    GatewayIntents.AllUnprivileged |
                    GatewayIntents.MessageContent |
                    GatewayIntents.GuildMessages |
                    GatewayIntents.Guilds
            });

            var host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Discord Services
                    services.AddSingleton(_client);
                    services.AddSingleton<CommandService>();
                    services.AddSingleton<InteractionService>();

                    /////////////////////////////////
                    //    ==-Command Logic-==     //
                    ///////////////////////////////

                    services.AddSingleton<CreateTournamentLogic>();

                    ////////////////////////////////

                    // Managers
                    services.AddSingleton<ConfigManager>();
                    services.AddSingleton<DataManager>();
                    services.AddSingleton<MatchManager>();
                    services.AddSingleton<TournamentManager>();

                    // Data Handlers
                    services.AddSingleton<DiscordCredentialHandler>();
                    services.AddSingleton<TournamentsDatabaseHandler>();
                })
                .Build();

            _services = host.Services;
            _configManager = _services.GetRequiredService<ConfigManager>();

            // Check discord token
            _configManager.SetDiscordTokenProcess();

            await RunBotAsync();
        }

        public async Task RunBotAsync()
        {
            _commands = _services.GetRequiredService<CommandService>();
            _interactionService = new InteractionService(_client.Rest);
            _commands.Log += Log;
            // Set up event handlers
            _client.Log += log =>
            {
                // Log if Discord requests a reconnect
                if (log.Exception is Discord.WebSocket.GatewayReconnectException)
                {
                    Console.WriteLine($"{DateTime.Now} - Gateway requested a reconnect.");
                }
                else
                {
                    Console.WriteLine(log.ToString());
                }
                return Task.CompletedTask;
            };
            _client.Ready += ClientReady;
            _client.InteractionCreated += HandleInteractionAsync;
            _client.MessageReceived += HandleCommandAsync;
            _client.Disconnected += exception =>
            {
                Console.WriteLine($"{DateTime.Now} - Bot disconnected: {exception?.Message ?? "Unknown reason"}");
                return Task.CompletedTask;
            };

            // Login and start the bot
            await _client.LoginAsync(TokenType.Bot, _configManager.GetDiscordToken());
            await _client.StartAsync();

            // Wait for Ready event
            var readyTask = new TaskCompletionSource<bool>();
            _client.Ready += () =>
            {
                readyTask.SetResult(true);
                return Task.CompletedTask;
            };

            await readyTask.Task;

            Console.WriteLine($"{DateTime.Now} - Bot logged in as: {_client.CurrentUser?.Username ?? "null"}");

            // Keep the bot running
            await Task.Delay(-1);
        }

        private async Task ClientReady()
        {
            // Register SlashCommand modules
            await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            // Check guild ID
            _configManager.SetGuildIdProcess();

            // Register commands to guild
            await _interactionService.RegisterCommandsToGuildAsync(_configManager.GetGuildId());
            Console.WriteLine($"{DateTime.Now} - Commands registered to guild {_configManager.GetGuildId()}");
        }

        private async Task HandleInteractionAsync(SocketInteraction interaction)
        {
            var context = new SocketInteractionContext(_client, interaction);
            var result = await _interactionService.ExecuteCommandAsync(context, _services);
        }

        private async Task HandleCommandAsync(SocketMessage socketMessage)
        {
            if (socketMessage is not SocketUserMessage message || message.Author.IsBot) return;

            int argPos = 0;
            // Get Command Prefix
            if (message.HasStringPrefix(_configManager.GetCommandPrefix(), ref argPos) ||
                message.HasMentionPrefix(_client.CurrentUser, ref argPos))
            {
                var context = new SocketCommandContext(_client, message);
                var result = await _commands.ExecuteAsync(context, argPos, _services);

                if (!result.IsSuccess)
                    Console.WriteLine($"{DateTime.Now} - Command Error: {result.ErrorReason}");
            }
        }

        private Task Log(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }
    }
}