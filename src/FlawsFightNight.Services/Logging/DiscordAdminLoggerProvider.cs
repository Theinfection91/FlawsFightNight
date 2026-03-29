using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace FlawsFightNight.Services.Logging
{
    public class DiscordAdminLoggerProvider : ILoggerProvider
    {
        private readonly DiscordAdminFeedService _sender;
        private readonly DiscordAdminLoggerOptions _options;

        public DiscordAdminLoggerProvider(DiscordAdminFeedService sender, IOptions<DiscordAdminLoggerOptions> options)
        {
            _sender = sender ?? throw new ArgumentNullException(nameof(sender));
            _options = options?.Value ?? new DiscordAdminLoggerOptions();
        }

        public ILogger CreateLogger(string categoryName) =>
            new DiscordAdminLogger(categoryName, _sender, _options.MinimumLevel);

        public void Dispose() { /* nothing to dispose; sender managed by host */ }
    }
}
