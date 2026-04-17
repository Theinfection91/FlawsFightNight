using Discord;
using FlawsFightNight.Core.Enums.UT2004;
using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.SettingsCommands.UT2004AdminCommands
{
    public class EloTraceAdminHandler : CommandHandler
    {
        private readonly EmbedFactory _embedFactory;
        private readonly MemberService _memberService;
        private readonly UT2004StatsService _ut2004StatsService;

        public EloTraceAdminHandler(EmbedFactory embedFactory, MemberService memberService, UT2004StatsService ut2004StatsService) : base("Admin Elo Trace")
        {
            _embedFactory = embedFactory;
            _memberService = memberService;
            _ut2004StatsService = ut2004StatsService;
        }

        public async Task<Embed> Handle(ulong discordId, string guid, UT2004GameMode uT2004GameMode)
        {
            if (!_ut2004StatsService.IsGuidInDatabase(guid))
            {
                return _embedFactory.ErrorEmbed(Name + " Error", $"The provided GUID `{guid}` was not found in the database.");
            }
            var eloTrace = await _ut2004StatsService.GetPlayerEloTrace(guid, uT2004GameMode);
            await _ut2004StatsService.SendTextFileDM(discordId, $"EloTrace_{uT2004GameMode}_{guid}.txt", eloTrace);
            return _embedFactory.GenericEmbed(Name + " Success", "Elo trace has been sent to your DMs.", Color.DarkBlue);
        }
    }
}
