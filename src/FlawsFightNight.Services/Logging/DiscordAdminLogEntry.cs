using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace FlawsFightNight.Services.Logging
{
    public sealed class DiscordAdminLogEntry
    {
        public string Category { get; init; }
        public LogLevel Level { get; init; }
        public EventId EventId { get; init; }
        public string Message { get; init; }
        public string? ExceptionMessage { get; init; }
        public string? ExceptionStackTrace { get; init; }
        public DateTimeOffset TimestampUtc { get; init; }
        public IReadOnlyDictionary<string, object?>? StateProperties { get; init; }
        public IReadOnlyList<string>? Scopes { get; init; }

        public DiscordAdminLogEntry(
            string category,
            LogLevel level,
            EventId eventId,
            string message,
            Exception? exception = null,
            IReadOnlyDictionary<string, object?>? stateProperties = null,
            IReadOnlyList<string>? scopes = null)
        {
            Category = category ?? throw new ArgumentNullException(nameof(category));
            Level = level;
            EventId = eventId;
            Message = message ?? string.Empty;
            ExceptionMessage = exception?.Message;
            ExceptionStackTrace = exception?.StackTrace;
            TimestampUtc = DateTimeOffset.UtcNow;
            StateProperties = stateProperties;
            Scopes = scopes;
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(ExceptionMessage))
                return $"{TimestampUtc:O} [{Level}] {Category} - {Message}";
            return $"{TimestampUtc:O} [{Level}] {Category} - {Message} | Exception: {ExceptionMessage}";
        }
    }
}