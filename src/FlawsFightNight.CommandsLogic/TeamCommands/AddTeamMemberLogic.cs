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
    public class AddTeamMemberLogic : CommandHandler
    {
        private readonly EmbedFactory _embedFactory;
        private readonly GitBackupService _gitBackupService;
        private readonly MemberService _memberManager;
        private readonly TeamService _teamService;
        private readonly TournamentService _tournamentService;
        public AddTeamMemberLogic(EmbedFactory embedFactory,
                                  GitBackupService gitBackupService,
                                  MemberService memberManager,
                                  TeamService teamService,
                                  TournamentService tournamentService) : base("Add Member")
        {
            _embedFactory = embedFactory;
            _gitBackupService = gitBackupService;
            _memberManager = memberManager;
            _teamService = teamService;
            _tournamentService = tournamentService;
        }

        public async Task<Embed> AddTeamMemberProcess(string teamName, List<IUser> membersToAdd)
        {
            if (_teamService.IsTeamNameUnique(teamName))
            {
                return _embedFactory.ErrorEmbed(Name, $"No team found with the name: {teamName}. Please check the team name and try again.");
            }
            var team = _teamService.GetTeamByName(teamName);
            var tournament = _tournamentService.GetTournamentFromTeamName(teamName);

            if (team.IsTeamFull(tournament.TeamSize))
            {
                                return _embedFactory.ErrorEmbed(Name, $"The team {teamName} is already full at {team.Members.Count} members.");
            }

            if (!team.CanAcceptAmountOfMembers(membersToAdd.Count, tournament.TeamSize))
            {
                return _embedFactory.ErrorEmbed(Name, $"The team {teamName} cannot accept {membersToAdd.Count} new members because it would exceed the team size of {tournament.TeamSize}. It currently has {team.Members.Count} members.");
            }

            var convertedMembersList = _memberManager.ConvertIUsersToMembers(membersToAdd);
            team.AddMembers(convertedMembersList);

            await _tournamentService.SaveAndReloadTournamentDataFiles(tournament);
            _gitBackupService.EnqueueBackup();

            return _embedFactory.GenericEmbed(Name, $"Successfully added {membersToAdd.Count} member(s) to the team '{teamName}' in the tournament '{tournament.Name}'.", Color.Blue);
        }
    }
}
