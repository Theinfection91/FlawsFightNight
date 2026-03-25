using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            if (formatter == null) return;

            string message = formatter(state, exception);
            var entry = new DiscordAdminLogEntry(
                category: _category,
                level: logLevel,
                eventId: eventId,
                message: message,
                exception: exception,
                stateProperties: null,
                scopes: null
            );

            // Fire-and-forget enqueue; the sender uses a bounded channel and background worker
            try
            {
                _service.Enqueue(entry);
            }
            catch
            {
                // Do not throw from logger; swallow or fallback to Console to avoid crashing app
                Console.WriteLine($"[DiscordAdminLogger][ENQUEUE FAILED] {logLevel} {message}");
            }
        }

        // Minimal NullScope to satisfy BeginScope
        private class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new NullScope();
            public void Dispose() { }
        }
    }
}
