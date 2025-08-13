using Discord;
using Discord.Interactions;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.TeamCommands
{
    public class RegisterTeamLogic : Logic
    {
        private EmbedManager _embedManager;
        private MemberManager _memberManager;
        private TournamentManager _tournamentManager;
        private TeamManager _teamManager;

        public RegisterTeamLogic(EmbedManager embedManager, MemberManager memberManager, TournamentManager tournamentManager, TeamManager teamManager) : base("Register Team")
        {
            _embedManager = embedManager;
            _memberManager = memberManager;
            _tournamentManager = tournamentManager;
            _teamManager = teamManager;
        }

        public Embed RegisterTeamProcess(SocketInteractionContext context, string teamName, string tournamentId, List<IUser> members)
        {
            // Check if the tournament exists, grab it if so
            if (!_tournamentManager.IsTournamentIdInDatabase(tournamentId))
            {
                return _embedManager.ErrorEmbed(Name, $"No tournament found with ID: {tournamentId}. Please check the ID and try again.");
            }
            Tournament? tournament = _tournamentManager.GetTournamentById(tournamentId);

            // Can register new teams if Ladder Tournament is running, but cannot register them to Round Robin Tournament or SE/DE Bracket once they have started
            if (_tournamentManager.CanAcceptNewTeams(tournament))
            {
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' can not accept new teams at this time.");
            }

            // Check if the team name is unique within the tournament
            if (!_teamManager.IsTeamNameUnique(teamName))
            {
                return _embedManager.ErrorEmbed(Name, $"The team name '{teamName}' is already taken in the tournament '{tournament.Name}'. Please choose a different name.");
            }

            // Check if member count is valid based on the tournament's team size
            if (!_memberManager.IsMemberCountCorrect(members.Count, tournament.TeamSize))
            {
                return _embedManager.ErrorEmbed(Name, $"The number of members ({members.Count}) does not match the required team size ({tournament.TeamSize}) for the tournament '{tournament.Name}'.");
            }

            // TODO Check if all members are valid and not already registered in the tournament

            // Convert IUser list to Member objects list
            List<Member> convertedMembersList = _memberManager.ConvertMembersListToObjects(members);

            // Create Team object
            Team newTeam = _teamManager.CreateTeam(teamName, convertedMembersList, tournament.Teams.Count + 1);

            // Add the new team to the tournament
            _tournamentManager.AddTeamToTournament(newTeam, tournament.Id);

            // TODO Check if tournament can be locked after adding the team
            if (_tournamentManager.CanTeamsBeLockedResolver(tournament))
            {
                _tournamentManager.SetCanTeamsBeLocked(tournament, true);
            }
            else
            {
                _tournamentManager.SetCanTeamsBeLocked(tournament, false);
            }

            // Save and reload the tournament database
            _tournamentManager.SaveAndReloadTournamentsDatabase();

            return _embedManager.TeamRegistrationSuccess(newTeam, tournament);
        }
    }
}
