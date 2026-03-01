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
using FlawsFightNight.Core.Helpers.UT2004;
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

        private bool _gitBackupSetupComplete = false;
        private bool _ftpSetupComplete = false;

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
                    services.AddSingleton<RemoveFTPCredentialsLogic>();
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
                    services.AddSingleton<OpenSkillRatingService>();
                    services.AddSingleton<UT2004StatsManager>();
                    services.AddSingleton<TeamManager>();
                    services.AddSingleton<TournamentManager>();

                    // Hosted services 
                    services.AddHostedService<LiveViewService>();
                    services.AddHostedService<FTPStatsService>();

                    // Data handlers
                    services.AddSingleton<DiscordCredentialHandler>();
                    services.AddSingleton<FTPCredentialHandler>();
                    services.AddSingleton<GitHubCredentialHandler>();
                    services.AddSingleton<PermissionsConfigHandler>();
                    services.AddSingleton<ProcessedLogNamesHandler>();
                    services.AddSingleton<StatLogMatchResultHandler>();
                    services.AddSingleton<TournamentDataHandler>();
                    services.AddSingleton<UserProfileHandler>();
                    services.AddSingleton<UT2004PlayerProfileHandler>();

                    // Parsers
                    services.AddSingleton<UT2004LogParser>();
                })
                .Build();

            using (var scope = host.Services.CreateScope())
            {
                var dataManager = scope.ServiceProvider.GetRequiredService<DataManager>();
                await dataManager.InitializeAsync();
            }

            // Prep config
            _services = host.Services;
            _configManager = _services.GetRequiredService<ConfigManager>();
            await _configManager.SetDiscordTokenProcess();
            await _configManager.SetGitBackupProcess();

            // Run interactive Git backup setup in background (clone/restore prompts)
            var gitBackupManager = _services.GetRequiredService<GitBackupManager>();
            var configManager = _services.GetRequiredService<ConfigManager>();
            while (!_gitBackupSetupComplete)
            {
                await gitBackupManager.RunInteractiveSetup();
                _gitBackupSetupComplete = configManager.IsGitPatTokenSet();
            }

            // Run interactive FTP setup in background (add credentials prompts)
            var ftpSetupManager = _services.GetRequiredService<ConfigManager>();
            while (!_ftpSetupComplete)
            {
                await ftpSetupManager.FTPSetupProcess();
                _ftpSetupComplete = ftpSetupManager.IsFTPCredentialsSet();
            }

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
            await _configManager.SetGuildIdProcess();
            await _interactionService.RegisterCommandsToGuildAsync(_configManager.GetGuildId());

            Console.WriteLine($"{DateTime.Now} - [Discord] Bot logged in as: {_client.CurrentUser}");
        }
    }
}
