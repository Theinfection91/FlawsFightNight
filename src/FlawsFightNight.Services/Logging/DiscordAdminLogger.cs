using Microsoft.Extensions.Logging;
using System;

namespace FlawsFightNight.Services.Logging
{
    public class DiscordAdminLogger : ILogger
    {
        private readonly string _category;
        private readonly DiscordAdminFeedService _service;
        private readonly LogLevel _minLevel;

        public DiscordAdminLogger(string category, DiscordAdminFeedService service, LogLevel minLevel)
        {
            _category = category;
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _minLevel = minLevel;
        }

        public IDisposable? BeginScope<TState>(TState state) => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            if (formatter is null) return;

            // Only forward to Discord if the caller explicitly tagged it as a feed event.
            if (!AdminFeedEvents.IsFeedEvent(eventId)) return;

            var entry = new DiscordAdminLogEntry(
                category: _category,
                level: logLevel,
                eventId: eventId,
                message: formatter(state, exception),
                exception: exception
            );

            try
            {
                _service.Enqueue(entry);
            }
            catch
            {

            }
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}
