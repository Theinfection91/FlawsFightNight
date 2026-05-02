using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.SettingsCommands.UT2004AdminCommands
{
    public class LastStatLogsHandler : CommandHandler
    {
        private readonly UT2004StatsService _ut2004StatsService;

        public LastStatLogsHandler(UT2004StatsService ut2004StatsService) : base("Last Stat Logs")
        {
            _ut2004StatsService = ut2004StatsService;
        }

        public async Task<string> GetLastStatLogsProcess(int amount, string serverName = null)
        {
            return await _ut2004StatsService.GetLastStatLogIDs(amount, serverName);
        }
    }
}