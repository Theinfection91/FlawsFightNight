using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.SettingsCommands.UT2004AdminCommands
{
    public class GetLogsByIDHandler : CommandHandler
    {
        private readonly UT2004StatsService _ut2004StatsService;
        public GetLogsByIDHandler(UT2004StatsService ut2004StatsService) : base("Get Logs By ID")
        {
            _ut2004StatsService = ut2004StatsService;
        }
        public async Task<string> GetLogsByID(SocketInteractionContext context, List<string> logIDs)
        {
            foreach (string logID in logIDs)
            {
                if (string.IsNullOrWhiteSpace(logID))
                    return $"Invalid log ID: '{logID}'. Please provide non-empty log IDs.";
            }

            var allLogs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (string logID in logIDs)
            {
                var result = await _ut2004StatsService.GetStatLogByID(logID);
                if (result != null)
                    allLogs[logID] = result[logID];
            }

            if (allLogs.Count == 0)
                return "No valid stat logs found for the provided ID(s).";

            await _ut2004StatsService.SendStatLogDM(context.User.Id, allLogs);
            return $"Sent {allLogs.Count} log(s) in DM.";
        }
    }
}
