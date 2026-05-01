using Discord;
using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.StatsCommands.UT2004StatsCommands
{
    public class DisplayMatchSummaryHandler : CommandHandler
    {
        private readonly EmbedFactory _embedFactory;
        private readonly UT2004StatsService _ut2004StatsService;
        public DisplayMatchSummaryHandler(EmbedFactory embedFactory, UT2004StatsService ut2004StatsService) : base("Display Match Summary")
        {
            _embedFactory = embedFactory;
            _ut2004StatsService = ut2004StatsService;
        }

        public async Task<(Embed Embed, string? FileContent, string? FileName)> Handle(string statLogID)
        {
            var canonicalId = _ut2004StatsService.TryResolveStatLogId(statLogID) ?? statLogID;

            if (!_ut2004StatsService.DoesStatLogExist(canonicalId))
                return (_embedFactory.ErrorEmbed(Name, $"No stat log found with the ID `{statLogID}`. Please check the ID and try again."), null, null);

            var matchSummary = await _ut2004StatsService.GetStatLogMatchSummary(canonicalId);
            if (matchSummary == null || string.IsNullOrWhiteSpace(matchSummary))
                return (_embedFactory.ErrorEmbed(Name, $"No match summary found for the stat log ID `{statLogID}`. Please check the ID and try again."), null, null);

            if (matchSummary.Length > 4096)
            {
                var embed = _embedFactory.GenericEmbed(Name + " Success", "", Color.DarkBlue);
                return (embed, matchSummary, $"{canonicalId}_summary.txt");
            }

            return (_embedFactory.GenericEmbed(Name + " Success", matchSummary, Color.DarkBlue), null, null);
        }
    }
}
