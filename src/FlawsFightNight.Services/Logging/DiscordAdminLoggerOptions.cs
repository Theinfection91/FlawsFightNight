using Microsoft.Extensions.Logging;
using System;

namespace FlawsFightNight.Services.Logging
{
    public class DiscordAdminLoggerOptions
    {
        // Master on/off switch for the provider
        public bool Enabled { get; set; } = true;

        // Minimum log level to forward to Discord
        public LogLevel MinimumLevel { get; set; } = LogLevel.Trace;

        // Maximum number of queued entries (bounded channel capacity)
        public int QueueCapacity { get; set; } = 1000;

        // How many entries to send in a single batch (if batching is used)
        public int BatchSize { get; set; } = 5;

        // How often to flush a partial batch
        public TimeSpan FlushPeriod { get; set; } = TimeSpan.FromSeconds(3);

        // Retry policy for failed sends
        public int MaxRetries { get; set; } = 3;
        public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromSeconds(2);

        // Include stack traces in Discord messages
        public bool IncludeExceptionStackTrace { get; set; } = true;

        // Optional: role ID or mention string to include on critical alerts (null = none)
        public string? Mention { get; set; } = null;
    }
}
