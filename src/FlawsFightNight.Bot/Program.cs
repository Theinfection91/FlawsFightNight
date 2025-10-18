using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using FlawsFightNight.Bot.Autocomplete;
using FlawsFightNight.CommandsLogic.MatchCommands;
using FlawsFightNight.CommandsLogic.SetCommands;
using FlawsFightNight.CommandsLogic.SettingsCommands;
using FlawsFightNight.CommandsLogic.TeamCommands;
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

        public ConfigManager configManager;
        public LiveViewManager liveViewManager;

        public static async Task Main(string[] args)
        {
            // Global exception hooks
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                Console.WriteLine($"[Unhandled Exception] {e.ExceptionObject}");

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                Console.WriteLine($"[Unobserved Task Exception] {e.Exception}");
                e.SetObserved();
            };

            var program = new Program();
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
                    // Discord services
                    services.AddSingleton(_client);
                    services.AddSingleton<CommandService>();
                    services.AddSingleton<InteractionService>();

                    // Autocomplete
                    services.AddSingleton<AutocompleteResolver>();

                    // == Command Logic ==
                    services.AddSingleton<AddTeamLossLogic>();
                    services.AddSingleton<AddTeamWinLogic>();
                    services.AddSingleton<AddTeamMemberLogic>();
                    services.AddSingleton<RemoveTeamLossLogic>();
                    services.AddSingleton<RemoveTeamWinLogic>();
                    services.AddSingleton<RemoveTeamMemberLogic>();
                    services.AddSingleton<AddDebugAdminLogic>();
                    services.AddSingleton<CancelChallengeLogic>();
                    services.AddSingleton<CreateTournamentLogic>();
                    services.AddSingleton<DeleteTeamLogic>();
                    services.AddSingleton<DeleteTournamentLogic>();
                    services.AddSingleton<EditMatchLogic>();
                    services.AddSingleton<EndTournamentLogic>();
                    services.AddSingleton<LockInRoundLogic>();
                    services.AddSingleton<LockTeamsLogic>();
                    services.AddSingleton<NextRoundLogic>();
                    services.AddSingleton<RegisterTeamLogic>();
                    services.AddSingleton<RemoveDebugAdminLogic>();
                    services.AddSingleton<RemoveMatchesChannelLogic>();
                    services.AddSingleton<RemoveStandingsChannelLogic>();
                    services.AddSingleton<RemoveTeamsChannelLogic>();
                    services.AddSingleton<ReportWinLogic>();
                    services.AddSingleton<SendChallengeLogic>();
                    services.AddSingleton<SetMatchesChannelLogic>();
                    services.AddSingleton<SetStandingsChannelLogic>();
                    services.AddSingleton<SetTeamsChannelLogic>();
                    services.AddSingleton<SetTeamRankLogic>();
                    services.AddSingleton<SetupRoundRobinTournamentLogic>();
                    services.AddSingleton<ShowAllTournamentsLogic>();
                    services.AddSingleton<StartTournamentLogic>();
                    services.AddSingleton<UnlockRoundLogic>();
                    services.AddSingleton<UnlockTeamsLogic>();

                    // Managers
                    services.AddSingleton<ConfigManager>();
                    services.AddSingleton<DataManager>();
                    services.AddSingleton<EmbedManager>();
                    services.AddSingleton<GitBackupManager>();
                    services.AddSingleton<LiveViewManager>();
                    services.AddSingleton<MatchManager>();
                    services.AddSingleton<MemberManager>();
                    services.AddSingleton<TeamManager>();
                    services.AddSingleton<TournamentManager>();

                    // Data handlers
                    services.AddSingleton<DiscordCredentialHandler>();
                    services.AddSingleton<GitHubCredentialHandler>();
                    services.AddSingleton<PermissionsConfigHandler>();
                    services.AddSingleton<TournamentsDatabaseHandler>();
                })
                .Build();

            _services = host.Services;
            configManager = _services.GetRequiredService<ConfigManager>();

            configManager.SetDiscordTokenProcess();
            configManager.SetGitBackupProcess();

            await RunBotAsync();
        }

        public async Task RunBotAsync()
        {
            _commands = _services.GetRequiredService<CommandService>();
            _interactionService = new InteractionService(_client);
            _commands.Log += Log;

            _client.Log += msg =>
            {
                if (msg.Exception is GatewayReconnectException)
                    Console.WriteLine($"{DateTime.Now} - Gateway requested a reconnect.");
                else
                    Console.WriteLine(msg.ToString());
                return Task.CompletedTask;
            };

            _client.Ready += ClientReady;
            _client.InteractionCreated += HandleInteractionAsync;
            _client.MessageReceived += HandleCommandAsync;
            _client.Disconnected += ex =>
            {
                Console.WriteLine($"{DateTime.Now} - Bot disconnected: {ex?.Message ?? "Unknown reason"}");
                return Task.CompletedTask;
            };

            // Attach Ready handler BEFORE starting the client
            var readyTask = new TaskCompletionSource<bool>();
            _client.Ready += () =>
            {
                readyTask.TrySetResult(true);
                return Task.CompletedTask;
            };

            await _client.LoginAsync(TokenType.Bot, configManager.GetDiscordToken());
            await _client.StartAsync();

            await readyTask.Task;

            // Initialize autocomplete after Ready
            var autoCompleteHandler = _services.GetRequiredService<AutocompleteResolver>();
            await autoCompleteHandler.InitializeAsync();

            Console.WriteLine($"{DateTime.Now} - Bot logged in as: {_client.CurrentUser?.Username ?? "null"}");

            liveViewManager = _services.GetRequiredService<LiveViewManager>();
            await Task.Delay(-1);
        }

        private async Task ClientReady()
        {
            await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            configManager.SetGuildIdProcess();
            await _interactionService.RegisterCommandsToGuildAsync(configManager.GetGuildId());
            Console.WriteLine($"{DateTime.Now} - Commands registered to guild {configManager.GetGuildId()}");
        }

        private async Task HandleInteractionAsync(SocketInteraction interaction)
        {
            var context = new SocketInteractionContext(_client, interaction);
            var result = await _interactionService.ExecuteCommandAsync(context, _services);
            if (!result.IsSuccess)
                Console.WriteLine($"{DateTime.Now} - Interaction Error: {result.ErrorReason}");
        }

        private async Task HandleCommandAsync(SocketMessage socketMessage)
        {
            if (socketMessage is not SocketUserMessage message || message.Author.IsBot) return;

            int argPos = 0;
            if (message.HasStringPrefix(configManager.GetCommandPrefix(), ref argPos) ||
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
