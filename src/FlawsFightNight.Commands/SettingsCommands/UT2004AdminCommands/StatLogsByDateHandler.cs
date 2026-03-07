using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.SettingsCommands.UT2004AdminCommands
{
    public class StatLogsByDateHandler : CommandHandler
    {
        private readonly UT2004StatsService _ut2004StatsService;

        public StatLogsByDateHandler(UT2004StatsService ut2004StatsService) : base("Stat Logs By Date")
        {
            _ut2004StatsService = ut2004StatsService;
        }

        public async Task<string> GetStatLogsByDate(DateTime date, string serverName = null)
        {
            return await _ut2004StatsService.GetStatLogIDsOnDate(date, serverName);
        }
    }
}
