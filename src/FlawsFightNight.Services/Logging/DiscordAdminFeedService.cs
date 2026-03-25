using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FlawsFightNight.Services.Logging
{
    public class DiscordAdminFeedService : BackgroundService
    {
        private readonly Channel<DiscordAdminLogEntry> _channel;
        private readonly DiscordSocketClient _discord;
        private readonly DataContext _dataContext;
        private readonly EmbedFactory _embedFactory;
        private readonly DiscordAdminLoggerOptions _options;

        public DiscordAdminFeedService(DiscordSocketClient discord, DataContext dataContext, EmbedFactory embedFactory, IOptions<DiscordAdminLoggerOptions> options)
        {
            _discord = discord ?? throw new ArgumentNullException(nameof(discord));
            _dataContext = dataContext ?? throw new ArgumentNullException(nameof(dataContext));
            _embedFactory = embedFactory ?? throw new ArgumentNullException(nameof(embedFactory));
            _options = options?.Value ?? new DiscordAdminLoggerOptions();
            _channel = Channel.CreateBounded<DiscordAdminLogEntry>(_options.QueueCapacity);
        }

        public ValueTask Enqueue(DiscordAdminLogEntry entry) => _channel.Writer.WriteAsync(entry);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var entry in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    var channelId = _dataContext.LiveViewChannelsFile?.AdminChannelFeedId ?? 0;
                    if (channelId == 0) continue;

                    var ch = _discord.GetChannel(channelId) as IMessageChannel;
                    if (ch == null) continue;

                    var embed = _embedFactory.FromLogEntry(entry);
                    await ch.SendMessageAsync(embed: embed);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DiscordAdminFeedService] Failed to send admin feed entry: {ex}");
                }
            }
        }
    }
}
