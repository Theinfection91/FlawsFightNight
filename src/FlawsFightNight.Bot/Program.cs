using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using FlawsFightNight.Bot.Autocomplete;
using FlawsFightNight.Commands.MatchCommands;
using FlawsFightNight.Commands.SettingsCommands;
using FlawsFightNight.Commands.SettingsCommands.UT2004AdminCommands;
using FlawsFightNight.Commands.StatsCommands.TournamentStatsCommands;
using FlawsFightNight.Commands.StatsCommands.UT2004StatsCommands;
using FlawsFightNight.Commands.TeamCommands;
using FlawsFightNight.Commands.TournamentCommands;
using FlawsFightNight.Core.Helpers.UT2004;
using FlawsFightNight.IO.Handlers;
using FlawsFightNight.Services;
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
        private AdminConfigurationService _adminConfigService;

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

            if (msg.HasStringPrefix(_adminConfigService.GetCommandPrefix(), ref pos) ||
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

                    /// Command Logic ///

                    // Match Commands
                    services.AddSingleton<CancelChallengeHandler>();
                    services.AddSingleton<EditMatchHandler>();
                    services.AddSingleton<ReportWinHandler>();
                    services.AddSingleton<SendChallengeHandler>();

                    // Settings Commands
                    services.AddSingleton<AddDebugAdminHandler>();
                    services.AddSingleton<AllowLogsByIDHandler>();
                    services.AddSingleton<GetLogsByIDHandler>();
                    services.AddSingleton<IgnoreLogsByIDHandler>();
                    services.AddSingleton<LastStatLogsHandler>();
                    services.AddSingleton<RegisterGuidToMemberHandler>();
                    services.AddSingleton<RemoveGuidFromMemberHandler>();
                    services.AddSingleton<RemoveDebugAdminHandler>();
                    services.AddSingleton<RemoveFTPCredentialsHandler>();
                    services.AddSingleton<RemoveMatchesChannelHandler>();
                    services.AddSingleton<RemoveStandingsChannelHandler>();
                    services.AddSingleton<RemoveTeamsChannelHandler>();
                    services.AddSingleton<StatLogsByDateHandler>();
                    services.AddSingleton<SetMatchesChannelHandler>();
                    services.AddSingleton<SetStandingsChannelHandler>();
                    services.AddSingleton<SetTeamsChannelHandler>();
                    services.AddSingleton<TagLogToMatchHandler>();
                    services.AddSingleton<UnTagLogToMatchHandler>();

                    // Stat Commands
                    services.AddSingleton<MyPlayerProfileHandler>();
                    services.AddSingleton<MyTournamentProfileHandler>();
                    services.AddSingleton<RegisterGuidHandler>();
                    services.AddSingleton<RemoveGuidHandler>();

                    // Team Commands
                    services.AddSingleton<RegisterTeamHandler>();
                    services.AddSingleton<SetTeamRankHandler>();
                    services.AddSingleton<AddTeamLossHandler>();
                    services.AddSingleton<AddTeamMemberHandler>();
                    services.AddSingleton<AddTeamWinHandler>();
                    services.AddSingleton<DeleteTeamHandler>();
                    services.AddSingleton<RemoveTeamLossHandler>();
                    services.AddSingleton<RemoveTeamMemberHandler>();
                    services.AddSingleton<RemoveTeamWinHandler>();

                    // Tournament Commands
                    services.AddSingleton<CreateTournamentHandler>();
                    services.AddSingleton<DeleteTournamentHandler>();
                    services.AddSingleton<EndTournamentHandler>();
                    services.AddSingleton<LockInRoundHandler>();
                    services.AddSingleton<LockTeamsHandler>();
                    services.AddSingleton<NextRoundHandler>();
                    services.AddSingleton<SetupRoundRobinTournamentHandler>();
                    services.AddSingleton<ShowAllTournamentsHandler>();
                    services.AddSingleton<StartTournamentHandler>();
                    services.AddSingleton<UnlockRoundHandler>();
                    services.AddSingleton<UnlockTeamsHandler>();

                    ///

                    // Services
                    services.AddSingleton<AdminConfigurationService>();
                    services.AddSingleton<DataContext>();
                    services.AddSingleton<EmbedFactory>();
                    services.AddSingleton<GitBackupService>();
                    services.AddSingleton<MatchService>();
                    services.AddSingleton<MemberService>();
                    services.AddSingleton<UT2004StatsService>();
                    services.AddSingleton<TeamService>();
                    services.AddSingleton<TournamentService>();

                    // Hosted services 
                    services.AddHostedService<LiveViewService>();
                    services.AddHostedService<FTPStatsService>();

                    // Data handlers
                    services.AddSingleton<DiscordCredentialHandler>();
                    services.AddSingleton<FTPCredentialHandler>();
                    services.AddSingleton<GitHubCredentialHandler>();
                    services.AddSingleton<PermissionsConfigHandler>();
                    services.AddSingleton<ProcessedLogNamesHandler>();
                    services.AddSingleton<StatLogIndexHandler>();
                    services.AddSingleton<StatLogMatchResultHandler>();
                    services.AddSingleton<TournamentDataHandler>();
                    services.AddSingleton<MemberProfileHandler>();
                    services.AddSingleton<UT2004PlayerProfileHandler>();

                    // UT2004 Helpers
                    services.AddSingleton<UT2004LogParser>();
                    services.AddSingleton<OpenSkillRatingService>();
                    services.AddSingleton<UTStatsDBEloRatingService>();
                    services.AddSingleton<SeamlessRatingsMapper>();
                })
                .Build();

            // Initialize data and stats services before starting hosted services to ensure they have the data they need when they start
            using (var scope = host.Services.CreateScope())
            {
                var dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
                var ut2004StatsService = scope.ServiceProvider.GetRequiredService<UT2004StatsService>();
                await dataContext.InitializeAsync();
                await ut2004StatsService.InitializeAsync();
            }

            // Prep config
            _services = host.Services;
            _adminConfigService = _services.GetRequiredService<AdminConfigurationService>();
            await _adminConfigService.SetDiscordTokenProcess();
            await _adminConfigService.SetGitBackupProcess();

            // Run interactive Git backup setup in background (clone/restore prompts)
            var gitBackupService = _services.GetRequiredService<GitBackupService>();
            var adminConfigService = _services.GetRequiredService<AdminConfigurationService>();
            while (!_gitBackupSetupComplete)
            {
                await gitBackupService.RunInteractiveSetup();
                _gitBackupSetupComplete = adminConfigService.IsGitPatTokenSet();
            }

            while (!_ftpSetupComplete)
            {
                await adminConfigService.FTPSetupProcess();
                _ftpSetupComplete = adminConfigService.IsFTPCredentialsSet();
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

            await _client.LoginAsync(TokenType.Bot, _adminConfigService.GetDiscordToken());
            await _client.StartAsync();

            await ready.Task;

            // Slash commands
            await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            await _adminConfigService.SetGuildIdProcess();
            await _interactionService.RegisterCommandsToGuildAsync(_adminConfigService.GetGuildId());

            Console.WriteLine($"{DateTime.Now} - [Discord] Bot logged in as: {_client.CurrentUser}");
        }
    }
}
