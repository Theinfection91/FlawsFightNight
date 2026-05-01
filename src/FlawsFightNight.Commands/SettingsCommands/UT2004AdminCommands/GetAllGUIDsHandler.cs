using Discord.Interactions;
using Discord.WebSocket;
using FlawsFightNight.Services;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.SettingsCommands.UT2004AdminCommands
{
    public class GetAllGUIDsHandler : CommandHandler
    {
        private readonly UT2004StatsService _ut2004StatsService;

        public GetAllGUIDsHandler(UT2004StatsService ut2004StatsService) : base("Get All GUIDs")
        {
            _ut2004StatsService = ut2004StatsService;
        }

        public async Task<string> GetAllGUIDsProcess(SocketInteractionContext context)
        {
            var profiles = _ut2004StatsService.GetAllPlayerProfiles();
            if (profiles.Count == 0)
                return "No player profiles found in the database.";

            var sb = new StringBuilder();
            sb.AppendLine($"UT2004 Player GUIDs — {profiles.Count} total | Generated {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine(new string('=', 72));

            foreach (var profile in profiles.OrderBy(p => p.CurrentName, StringComparer.OrdinalIgnoreCase))
                sb.AppendLine($"{profile.Guid}  |  {profile.CurrentName}");

            await _ut2004StatsService.SendTextFileDM(context.User.Id, "ut2004_guid_list.txt", sb.ToString());
            return $"Sent a list of {profiles.Count} GUID(s) to your DMs.";
        }
    }
}