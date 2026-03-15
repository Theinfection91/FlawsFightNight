using Discord;
using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.SettingsCommands.UT2004AdminCommands
{
    public class UnTagLogToMatchHandler : CommandHandler
    {
        private readonly EmbedFactory _embedFactory;
        private readonly GitBackupService _gitBackupService;
        private readonly UT2004StatsService _ut2004StatsService;
        public UnTagLogToMatchHandler(EmbedFactory embedFactory, GitBackupService gitBackupService, UT2004StatsService ut2004StatsService) : base("UnTag Log From Match")
        {
            _embedFactory = embedFactory;
            _gitBackupService = gitBackupService;
            _ut2004StatsService = ut2004StatsService;
        }
        public async Task<Embed> UnTagLogFromMatchProcess(string statLogId)
        {
            if (!_ut2004StatsService.DoesStatLogExist(statLogId))
                return _embedFactory.ErrorEmbed(Name, $"The provided stat log ID `{statLogId}` does not exist in the index.");
            
            return _embedFactory.GenericEmbed(Name + " Success", $"The stat log `{statLogId}` has been successfully untagged from its tournament match.", Color.DarkBlue);
        }
    }
}
