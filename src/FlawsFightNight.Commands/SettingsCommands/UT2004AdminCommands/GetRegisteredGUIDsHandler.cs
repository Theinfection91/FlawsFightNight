using Discord.Interactions;
using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.SettingsCommands.UT2004AdminCommands
{
    public class GetRegisteredGUIDsHandler : CommandHandler
    {
        private readonly MemberService _memberService;
        private readonly UT2004StatsService _ut2004StatsService;

        public GetRegisteredGUIDsHandler(MemberService memberService, UT2004StatsService ut2004StatsService) : base("Get Registered GUIDs")
        {
            _memberService = memberService;
            _ut2004StatsService = ut2004StatsService;
        }

        public async Task<string> Handle(SocketInteractionContext context)
        {
            var registeredGuidReport = _memberService.GetAllRegisteredUT2004GUIDsFromMemberProfiles();

            await _ut2004StatsService.SendTextFileDM(context.User.Id, "registered_ut2004_guids.txt", registeredGuidReport);

            return $"Sent a list of registered GUIDs to your DMs.";
        }
    }
}
