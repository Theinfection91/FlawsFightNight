using Discord;
using FlawsFightNight.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.TeamCommands
{
    public class AddTeamMemberLogic : Logic
    {
        private readonly EmbedManager _embedManager;
        private readonly GitBackupManager _gitBackupManager;
        private readonly MemberManager _memberManager;
        private readonly TeamManager _teamManager;
        private readonly TournamentManager _tournamentManager;
        public AddTeamMemberLogic(EmbedManager embedManager,
                                  GitBackupManager gitBackupManager,
                                  MemberManager memberManager,
                                  TeamManager teamManager,
                                  TournamentManager tournamentManager) : base("Add Member")
        {
            _embedManager = embedManager;
            _gitBackupManager = gitBackupManager;
            _memberManager = memberManager;
            _teamManager = teamManager;
            _tournamentManager = tournamentManager;
        }

        public async Task<Embed> AddTeamMemberProcess(string teamName, List<IUser> membersToAdd)
        {
            if (_teamManager.IsTeamNameUnique(teamName))
            {
                return _embedManager.ErrorEmbed(Name, $"No team found with the name: {teamName}. Please check the team name and try again.");
            }
            var team = _teamManager.GetTeamByName(teamName);
            var tournament = _tournamentManager.GetTournamentFromTeamName(teamName);

            if (team.IsTeamFull(tournament.TeamSize))
            {
                                return _embedManager.ErrorEmbed(Name, $"The team {teamName} is already full at {team.Members.Count} members.");
            }

            if (!team.CanAcceptAmountOfMembers(membersToAdd.Count, tournament.TeamSize))
            {
                return _embedManager.ErrorEmbed(Name, $"The team {teamName} cannot accept {membersToAdd.Count} new members because it would exceed the team size of {tournament.TeamSize}. It currently has {team.Members.Count} members.");
            }

            var convertedMembersList = _memberManager.ConvertIUsersToMembers(membersToAdd);
            team.AddMembers(convertedMembersList);

            await _tournamentManager.SaveAndReloadTournamentDataFiles(tournament);
            _gitBackupManager.EnqueueBackup();

            return _embedManager.GenericEmbed(Name, $"Successfully added {membersToAdd.Count} member(s) to the team '{teamName}' in the tournament '{tournament.Name}'.", Color.Blue);
        }
    }
}
