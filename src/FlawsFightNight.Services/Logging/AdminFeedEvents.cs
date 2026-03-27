using Microsoft.Extensions.Logging;
using System.Collections.Frozen;

namespace FlawsFightNight.Services.Logging
{
    /// <summary>
    /// Defines EventIds that are forwarded to the Discord admin feed channel in addition to the normal console log.
    /// Usage: _logger.LogWarning(AdminFeedEvents.FtpSetupStarted, "message {Arg}", arg);
    /// </summary>
    public static class AdminFeedEvents
    {
        // ── FTP ────────────────────────────────────────────────────────────
        public static readonly EventId FtpSetupStarted    = new(1001, nameof(FtpSetupStarted));
        public static readonly EventId FtpSetupCompleted  = new(1002, nameof(FtpSetupCompleted));
        public static readonly EventId FtpSetupFailed     = new(1003, nameof(FtpSetupFailed));
        public static readonly EventId FtpSetupCancelled  = new(1004, nameof(FtpSetupCancelled));

        // ── Git Backup ─────────────────────────────────────────────────────
        public static readonly EventId GitBackupFailed    = new(2001, nameof(GitBackupFailed));

        // ── Permissions / Admin Actions ────────────────────────────────────
        public static readonly EventId AdminActionTaken   = new(3001, nameof(AdminActionTaken));
        public static readonly EventId UnauthorizedAccess = new(3002, nameof(UnauthorizedAccess));

        // ── Stats / Data ───────────────────────────────────────────────────
        public static readonly EventId StatLogProcessed   = new(4001, nameof(StatLogProcessed));
        public static readonly EventId StatLogFailed      = new(4002, nameof(StatLogFailed));
        public static readonly EventId PlayerProfilesRebuilt = new(4003, nameof(PlayerProfilesRebuilt));

        // ── Internal lookup ───────────────────────────────────────────────
        private static readonly FrozenSet<int> _feedIds = new[]
        {
            1001, 1002, 1003, 1004,
            2001,
            3001, 3002,
            4001, 4002, 4003
        }.ToFrozenSet();

        public static bool IsFeedEvent(EventId eventId) => _feedIds.Contains(eventId.Id);
    }
}