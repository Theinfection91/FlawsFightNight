using Discord;
using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.SettingsCommands.UT2004AdminCommands
{
    public class TagLogToMatchHandler : CommandHandler
    {
        public GitBackupService _gitBackupService;
        public UT2004StatsService _ut2004StatsService;
        public TagLogToMatchHandler(GitBackupService gitBackupService, UT2004StatsService ut2004StatsService) : base("Tag Log To Match")
        {
            _gitBackupService = gitBackupService;
            _ut2004StatsService = ut2004StatsService;
        }

        public async Task<Embed> TagLogToMatchProcess(string statLogId, string tournamentId, string matchId)
        {
            return null;
        }
    }
}
