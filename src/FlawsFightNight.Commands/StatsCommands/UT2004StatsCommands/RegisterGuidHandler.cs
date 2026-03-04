using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.StatsCommands.UT2004StatsCommands
{
    public class RegisterGuidHandler : CommandHandler
    {
        private readonly EmbedFactory _embedFactory;
        private readonly GitBackupService _gitBackupService;
        private readonly MemberService _memberService;
        private readonly UT2004StatsService _ut2004StatsService;
        public RegisterGuidHandler(EmbedFactory embedFactory,
                                 GitBackupService gitBackupService,
                                 MemberService memberService,
                                 UT2004StatsService ut2004StatsService) : base("Register GUID")
        {
            _embedFactory = embedFactory;
            _gitBackupService = gitBackupService;
            _memberService = memberService;
            _ut2004StatsService = ut2004StatsService;
        }
        public async Task<Embed> RegisterGuidProcess(SocketInteractionContext context, string guid)
        {
            if (_memberService.IsUT2004GUIDRegistered(guid))
            {
                return _embedFactory.ErrorEmbed(Name, $"The GUID `{guid}` is already registered to another account. Please check the GUID and try again.");
            }

            var memberProfile = _memberService.GetMemberProfile(context.User.Id)!;
            if (memberProfile == null)
            {
                return _embedFactory.ErrorEmbed(Name, "An error occurred while retrieving your member profile. Please try again later.");
            }

            if (memberProfile.RegisteredUT2004GUIDs.Count >= 1)
            {
                // Users may only register one GUID. To register more for SeamlessRatings an admin must do it for them
                return _embedFactory.ErrorEmbed(Name, $"You have already registered a GUID to your account. Users may only register one GUID. If you need to register additional GUIDs for SeamlessRatings, please contact an administrator.");
            }
            memberProfile.RegisterUT2004GUID(guid);
            

            //var utProfile = _memberService.GetUT2004PlayerProfile(guid);
            //if (utProfile == null) 
            //{
            //    return _embedFactory.ErrorEmbed(Name, $"An error occurred while retrieving the UT2004 profile for GUID `{guid}`. Please ensure the GUID is correct and try again.");
            //}

            await _memberService.SaveAndReloadMemberProfiles();
            _gitBackupService.EnqueueBackup();

            return _embedFactory.GenericEmbed(Name, $"The GUID `{guid}` has been successfully registered to your account.", Color.Blue);
        }
    }
}
