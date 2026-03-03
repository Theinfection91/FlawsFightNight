using Discord;
using FlawsFightNight.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.TeamCommands
{
    public class RemoveTeamMemberLogic : Logic
    {
        private readonly EmbedManager _embedManager;
        private readonly GitBackupManager _gitBackupManager;
        private readonly MemberManager _memberManager;
        private readonly TeamManager _teamManager;
        private readonly TournamentManager _tournamentManager;
        public RemoveTeamMemberLogic(EmbedManager embedManager,
                                     GitBackupManager gitBackupManager,
                                     MemberManager memberManager,
                                     TeamManager teamManager,
                                     TournamentManager tournamentManager) : base("Remove Member")
        {
            _embedManager = embedManager;
            _gitBackupManager = gitBackupManager;
            _memberManager = memberManager;
            _teamManager = teamManager;
            _tournamentManager = tournamentManager;
        }

        public async Task<Embed> RemoveTeamMemberProcess(string teamName, List<IUser> membersToRemove)
        {
            if (_teamManager.IsTeamNameUnique(teamName))
            {
                return _embedManager.ErrorEmbed(Name, $"No team found with the name: {teamName}. Please check the team name and try again.");
            }
            var team = _teamManager.GetTeamByName(teamName);
            var tournament = _tournamentManager.GetTournamentFromTeamName(teamName);

            var convertedMembersList = _memberManager.ConvertIUsersToMembers(membersToRemove);
            if (!team.ContainsMembers(convertedMembersList, out var missingMembers))
            {
                return _embedManager.ErrorEmbed(Name, $"The team '{teamName}' does not contain all of the specified members to remove. Missing members: {string.Join(", ", missingMembers.Select(m => m.DisplayName))}. Please check the member list and try again.");
            }
            team.RemoveMembers(convertedMembersList);

            await _tournamentManager.SaveAndReloadTournamentDataFiles(tournament);
            _gitBackupManager.EnqueueBackup();

            return _embedManager.GenericEmbed(Name, $"Successfully removed {membersToRemove.Count} member(s) from the team '{teamName}' in the tournament '{tournament.Name}'.", Color.Blue);
        }
    }
}
