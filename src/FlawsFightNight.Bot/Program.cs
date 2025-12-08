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
using Hangfire;
using Hangfire.MemoryStorage;
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

        private ConfigManager _configManager;
        private LiveViewManager _liveViewManager;
        private LiveViewService _liveViewService;

        public static async Task Main(string[] args)
        {
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

        private async Task HandleInteractionAsync(SocketInteraction interaction)
        {
            try
            {
                var context = new SocketInteractionContext(_client, interaction);
                var result = await _interactionService.ExecuteCommandAsync(context, _services);
                if (!result.IsSuccess)
                    Console.WriteLine($"{DateTime.Now} - Interaction Error: {result.ErrorReason}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Interaction Exception] {ex}");
            }
        }

        private async Task HandleCommandAsync(SocketMessage socketMessage)
        {
            if (socketMessage is not SocketUserMessage message || message.Author.IsBot) return;

            int argPos = 0;
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
                    services.AddSingleton<AutocompleteCache>();

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
                    services.AddSingleton<MatchManager>();
                    services.AddSingleton<MemberManager>();
                    services.AddSingleton<TeamManager>();
                    services.AddSingleton<TournamentManager>();

                    // Services
                    services.AddHostedService<LiveViewService>();

                    // Data handlers
                    services.AddSingleton<DiscordCredentialHandler>();
                    services.AddSingleton<GitHubCredentialHandler>();
                    services.AddSingleton<PermissionsConfigHandler>();
                    services.AddSingleton<TournamentsDatabaseHandler>();
                })
                .Build();

            _services = host.Services;
            _configManager = _services.GetRequiredService<ConfigManager>();

            _configManager.SetDiscordTokenProcess();
            _configManager.SetGitBackupProcess();

            _commands = _services.GetRequiredService<CommandService>();
            _interactionService = new InteractionService(_client);

            _commands.Log += Log;
            _client.Log += Log;

            _client.Disconnected += ex =>
            {
                Console.WriteLine($"{DateTime.Now} - Bot disconnected: {ex?.Message ?? "Unknown reason"}");
                return Task.CompletedTask;
            };

            var readyTask = new TaskCompletionSource<bool>();
            _client.Ready += () =>
            {
                readyTask.TrySetResult(true);
                return Task.CompletedTask;
            };

            _client.InteractionCreated += HandleInteractionAsync;
            _client.MessageReceived += HandleCommandAsync;

            await _client.LoginAsync(TokenType.Bot, _configManager.GetDiscordToken());
            await _client.StartAsync();

            await readyTask.Task;

            //_ = Task.Run(async () =>
            //{
                await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
                _configManager.SetGuildIdProcess();
                await _interactionService.RegisterCommandsToGuildAsync(_configManager.GetGuildId());
                Console.WriteLine($"{DateTime.Now} - Commands registered to guild {_configManager.GetGuildId()}");
            //});

            Console.WriteLine($"{DateTime.Now} - Bot logged in as: {_client.CurrentUser?.Username ?? "null"}");

            // Start the host, which runs LiveViewService automatically
            Console.WriteLine("Bot running...");
            await host.RunAsync();
            //await host.StartAsync();
            
            //await Task.Delay(Timeout.Infinite); // keep main thread alive
        }

        public static async Task UpdateTournamentsAsync(DataManager dataManager, EmbedManager embedManager, GitBackupManager gitBackupManager)
        {
            var tournaments = dataManager.TournamentsDatabaseFile.Tournaments.Where(t => t != null).ToList();
            Console.WriteLine($"[LiveView] Found {tournaments.Count} tournaments to update.");

            foreach (var t in tournaments)
            {
                Console.WriteLine($"[LiveView] Starting update for tournament {t.Id}");
                try
                {
                    var client = dataManager.DiscordClient;

                    if (t.MatchesChannelId != 0)
                    {
                        Console.WriteLine($"[LiveView] Updating Matches for tournament {t.Id}");
                        var channel = client.GetChannel(t.MatchesChannelId) as IMessageChannel;
                        if (channel != null)
                        {
                            var embed = embedManager.MatchesLiveViewResolver(t);
                            if (t.MatchesMessageId != 0)
                            {
                                Console.WriteLine($"[LiveView] Modifying existing Matches message {t.MatchesMessageId}");
                                var existing = await channel.GetMessageAsync(t.MatchesMessageId) as IUserMessage;
                                if (existing != null) await existing.ModifyAsync(m => m.Embed = embed);
                            }
                            else
                            {
                                Console.WriteLine($"[LiveView] Sending new Matches message");
                                var newMsg = await channel.SendMessageAsync(embed: embed);
                                t.MatchesMessageId = newMsg.Id;
                            }
                        }
                        else Console.WriteLine($"[LiveView] Matches channel {t.MatchesChannelId} not found");
                    }

                    if (t.StandingsChannelId != 0)
                    {
                        Console.WriteLine($"[LiveView] Updating Standings for tournament {t.Id}");
                        var channel = client.GetChannel(t.StandingsChannelId) as IMessageChannel;
                        if (channel != null)
                        {
                            var embed = embedManager.StandingsLiveViewResolver(t);
                            if (t.StandingsMessageId != 0)
                            {
                                Console.WriteLine($"[LiveView] Modifying existing Standings message {t.StandingsMessageId}");
                                var existing = await channel.GetMessageAsync(t.StandingsMessageId) as IUserMessage;
                                if (existing != null) await existing.ModifyAsync(m => m.Embed = embed);
                            }
                            else
                            {
                                Console.WriteLine($"[LiveView] Sending new Standings message");
                                var newMsg = await channel.SendMessageAsync(embed: embed);
                                t.StandingsMessageId = newMsg.Id;
                            }
                        }
                        else Console.WriteLine($"[LiveView] Standings channel {t.StandingsChannelId} not found");
                    }

                    if (t.TeamsChannelId != 0)
                    {
                        Console.WriteLine($"[LiveView] Updating Teams for tournament {t.Id}");
                        var channel = client.GetChannel(t.TeamsChannelId) as IMessageChannel;
                        if (channel != null)
                        {
                            var embed = embedManager.TeamsLiveView(t);
                            if (t.TeamsMessageId != 0)
                            {
                                Console.WriteLine($"[LiveView] Modifying existing Teams message {t.TeamsMessageId}");
                                var existing = await channel.GetMessageAsync(t.TeamsMessageId) as IUserMessage;
                                if (existing != null) await existing.ModifyAsync(m => m.Embed = embed);
                            }
                            else
                            {
                                Console.WriteLine($"[LiveView] Sending new Teams message");
                                var newMsg = await channel.SendMessageAsync(embed: embed);
                                t.TeamsMessageId = newMsg.Id;
                            }
                        }
                        else Console.WriteLine($"[LiveView] Teams channel {t.TeamsChannelId} not found");
                    }

                    Console.WriteLine($"[LiveView] Finished update for tournament {t.Id}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[LiveView] Tournament {t.Id} update failed: {ex}");
                }
            }

            Console.WriteLine("[LiveView] All tournaments updated.");

            // Optionally save and backup
            // dataManager.SaveAndReloadTournamentsDatabase();
            // gitBackupManager.CopyAndBackupFilesToGit();
        }

        //public async Task RunBotAsync()
        //{
        //    _commands = _services.GetRequiredService<CommandService>();
        //    _interactionService = new InteractionService(_client);
        //    _commands.Log += Log;
        //    _client.Log += Log;

        //    _client.Disconnected += ex =>
        //    {
        //        Console.WriteLine($"{DateTime.Now} - Bot disconnected: {ex?.Message ?? "Unknown reason"}");
        //        return Task.CompletedTask;
        //    };

        //    // Ready handling
        //    var readyTask = new TaskCompletionSource<bool>();
        //    _client.Ready += () =>
        //    {
        //        readyTask.TrySetResult(true);
        //        return Task.CompletedTask;
        //    };

        //    _client.InteractionCreated += HandleInteractionAsync;
        //    _client.MessageReceived += HandleCommandAsync;

        //    await _client.LoginAsync(TokenType.Bot, _configManager.GetDiscordToken());
        //    await _client.StartAsync();

        //    await readyTask.Task; // Wait until Discord signals Ready

        //    // Fire-and-forget module registration and guild commands
        //    _ = Task.Run(async () =>
        //    {
        //        await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        //        _configManager.SetGuildIdProcess();
        //        await _interactionService.RegisterCommandsToGuildAsync(_configManager.GetGuildId());
        //        Console.WriteLine($"{DateTime.Now} - Commands registered to guild {_configManager.GetGuildId()}");
        //    });

        //    Console.WriteLine($"{DateTime.Now} - Bot logged in as: {_client.CurrentUser?.Username ?? "null"}");

        //    // Grab binder and start hosting
        //    var host = _services.GetRequiredService<IHost>();
        //    if (_client.LoginState == LoginState.LoggedIn)
        //    {
        //        await host.StartAsync();
        //    }

        //    await Task.Delay(Timeout.InfiniteTimeSpan); // keep bot running
        //}
    }
}
