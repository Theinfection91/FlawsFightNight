using Discord;
using Discord.Interactions;
using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.SettingsCommands.UT2004AdminCommands
{
    public class IgnoreLogsByIDHandler : CommandHandler
    {
        private readonly EmbedFactory _embedFactory;
        private readonly GitBackupService _gitBackupService;
        private readonly UT2004StatsService _ut2004StatsService;

        public IgnoreLogsByIDHandler(EmbedFactory embedFactory, GitBackupService gitBackupService, UT2004StatsService ut2004StatsService) : base("Ignore UT2004 Logs By ID")
        {
            _embedFactory = embedFactory;
            _gitBackupService = gitBackupService;
            _ut2004StatsService = ut2004StatsService;
        }

        public async Task<Embed> IgnoreLogsByIDProcess(SocketInteractionContext context, List<string> statLogIDs)
        {
            string adminName = context.User.GlobalName ?? context.User.Username;
            var (succeeded, alreadyIgnored, notFound) = await _ut2004StatsService.IgnoreStatLogsByID(statLogIDs, context.User.Id, adminName);

            if (succeeded.Count == 0 && alreadyIgnored.Count == 0 && notFound.Count > 0)
                return _embedFactory.ErrorEmbed(Name, $"None of the provided IDs were found in the stat log index.\n\n❌ **Not Found:**\n{string.Join("\n", notFound.Select(id => $"• `{id}`"))}");

            var sb = new StringBuilder();

            if (succeeded.Count > 0)
            {
                sb.AppendLine($"✅ **Newly Ignored ({succeeded.Count}):**");
                foreach (var id in succeeded)
                    sb.AppendLine($"• `{id}`");
            }

            if (alreadyIgnored.Count > 0)
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.AppendLine($"⚠️ **Already Ignored ({alreadyIgnored.Count}):**");
                foreach (var id in alreadyIgnored)
                    sb.AppendLine($"• `{id}`");
            }

            if (notFound.Count > 0)
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.AppendLine($"❌ **Not Found ({notFound.Count}):**");
                foreach (var id in notFound)
                    sb.AppendLine($"• `{id}`");
            }

            if (succeeded.Count > 0)
            {
                sb.AppendLine();
                await _ut2004StatsService.RebuildPlayerProfiles();
                sb.AppendLine("✅ Player profiles rebuilt successfully.");
                _gitBackupService.EnqueueBackup();
            }

            Color resultColor = succeeded.Count > 0 ? Color.Green : Color.LightGrey;
            return _embedFactory.GenericEmbed(Name, sb.ToString().TrimEnd(), resultColor, $"Actioned by {adminName}");
        }
    }
}
