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
        private IServiceProvider _services;
        private DiscordSocketClient _client;
        private CommandService _commands;
        private InteractionService _interactionService;
        private ConfigManager _configManager;

        public static async Task Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                Console.WriteLine($"[Unhandled Exception] {e.ExceptionObject}");

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                Console.WriteLine($"[Unobserved Task Exception] {e.Exception}");
                e.SetObserved();
            };

            await new Program().RunAsync();
        }

        private Task Log(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        private async Task HandleInteractionAsync(SocketInteraction interaction)
        {
            try
            {
                var context = new SocketInteractionContext(_client, interaction);
                var result = await _interactionService.ExecuteCommandAsync(context, _services);

                if (!result.IsSuccess)
                    Console.WriteLine($"{DateTime.Now} - [Discord] Interaction Error: {result.ErrorReason}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Interaction Exception] {ex}");
            }
        }

        private async Task HandleMessageAsync(SocketMessage raw)
        {
            if (raw is not SocketUserMessage msg) return;
            if (msg.Author.IsBot) return;

            int pos = 0;

            if (msg.HasStringPrefix(_configManager.GetCommandPrefix(), ref pos) ||
                msg.HasMentionPrefix(_client.CurrentUser, ref pos))
            {
                var ctx = new SocketCommandContext(_client, msg);
                var result = await _commands.ExecuteAsync(ctx, pos, _services);

                if (!result.IsSuccess)
                    Console.WriteLine($"{DateTime.Now} - [Discord] Command Error: {result.ErrorReason}");
            }
        }

        public async Task RunAsync()
        {
            // Discord client setup
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info,
                GatewayIntents =
                    GatewayIntents.Guilds |
                    GatewayIntents.GuildMessages |
                    GatewayIntents.MessageContent
            });

            // Host and DI setup
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton(_client);
                    services.AddSingleton<CommandService>();
                    services.AddSingleton<InteractionService>();

                    // Autocomplete
                    services.AddSingleton<AutocompleteCache>();

                    // Command Logic
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
                    services.AddSingleton<MatchManager>();
                    services.AddSingleton<MemberManager>();
                    services.AddSingleton<TeamManager>();
                    services.AddSingleton<TournamentManager>();

                    // Hosted services
                    services.AddHostedService<LiveViewService>();

                    // Data handlers
                    services.AddSingleton<DiscordCredentialHandler>();
                    services.AddSingleton<GitHubCredentialHandler>();
                    services.AddSingleton<PermissionsConfigHandler>();

                    // New Tournament System Data Handler
                    services.AddSingleton<TournamentDataHandler>();
                })
                .Build();

            // Prep config
            _services = host.Services;
            _configManager = _services.GetRequiredService<ConfigManager>();
            _configManager.SetDiscordTokenProcess();
            _configManager.SetGitBackupProcess();

            // Discord services
            _commands = _services.GetRequiredService<CommandService>();
            _interactionService = new InteractionService(_client);

            _commands.Log += Log;
            _client.Log += Log;

            _client.Disconnected += ex =>
            {
                Console.WriteLine($"{DateTime.Now} - [Discord] Bot disconnected: {ex?.Message ?? "Unknown"}");
                return Task.CompletedTask;
            };

            _client.InteractionCreated += HandleInteractionAsync;
            _client.MessageReceived += HandleMessageAsync;

            // Start hosted services
            await host.StartAsync();

            // Start Discord in background task
            _ = Task.Run(async () => await RunDiscordAsync());

            // Display program assembly version
            Console.WriteLine($"{DateTime.Now} - [Program] {Assembly.GetExecutingAssembly().GetName().Name} v{Assembly.GetExecutingAssembly().GetName().Version} running...");
            await Task.Delay(Timeout.Infinite);
        }

        private async Task RunDiscordAsync()
        {
            var ready = new TaskCompletionSource<bool>();

            _client.Ready += () =>
            {
                ready.TrySetResult(true);
                return Task.CompletedTask;
            };

            await _client.LoginAsync(TokenType.Bot, _configManager.GetDiscordToken());
            await _client.StartAsync();

            await ready.Task;

            // Slash commands
            await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            _configManager.SetGuildIdProcess();
            await _interactionService.RegisterCommandsToGuildAsync(_configManager.GetGuildId());

            Console.WriteLine($"{DateTime.Now} - [Discord] Bot logged in as: {_client.CurrentUser}");
        }
    }
}
