using Discord;
using FlawsFightNight.Commands;
using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.TeamCommands
{
    public class RemoveTeamMemberHandler : CommandHandler
    {
        private readonly EmbedFactory _embedFactory;
        private readonly GitBackupService _gitBackupService;
        private readonly MemberService _memberManager;
        private readonly TeamService _teamService;
        private readonly TournamentService _tournamentService;
        public RemoveTeamMemberHandler(EmbedFactory embedFactory,
                                     GitBackupService gitBackupService,
                                     MemberService memberManager,
                                     TeamService teamService,
                                     TournamentService tournamentService) : base("Remove Member")
        {
            _embedFactory = embedFactory;
            _gitBackupService = gitBackupService;
            _memberManager = memberManager;
            _teamService = teamService;
            _tournamentService = tournamentService;
        }

        public async Task<Embed> RemoveTeamMemberProcess(string teamName, List<IUser> membersToRemove)
        {
            if (_teamService.IsTeamNameUnique(teamName))
            {
                return _embedFactory.ErrorEmbed(Name, $"No team found with the name: {teamName}. Please check the team name and try again.");
            }
            var team = _teamService.GetTeamByName(teamName);
            var tournament = _tournamentService.GetTournamentFromTeamName(teamName);

            var convertedMembersList = _memberManager.ConvertIUsersToMembers(membersToRemove);
            if (!team.ContainsMembers(convertedMembersList, out var missingMembers))
            {
                return _embedFactory.ErrorEmbed(Name, $"The team '{teamName}' does not contain all of the specified members to remove. Missing members: {string.Join(", ", missingMembers.Select(m => m.DisplayName))}. Please check the member list and try again.");
            }
            team.RemoveMembers(convertedMembersList);

            await _tournamentService.SaveAndReloadTournamentDataFiles(tournament);
            _gitBackupService.EnqueueBackup();

            return _embedFactory.GenericEmbed(Name, $"Successfully removed {membersToRemove.Count} member(s) from the team '{teamName}' in the tournament '{tournament.Name}'.", Color.Blue);
        }
    }
}
