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
        private MemberManager _memberManager;
        private TournamentManager _tournamentManager;
        private TeamManager _teamManager;

        public RegisterTeamLogic(MemberManager memberManager, TournamentManager tournamentManager, TeamManager teamManager) : base("Register Team")
        {
            _memberManager = memberManager;
            _tournamentManager = tournamentManager;
            _teamManager = teamManager;
        }

        public string RegisterTeamProcess(SocketInteractionContext context, string teamName, string tournamentId, List<IUser> members)
        {
            // Check if the tournament exists, grab it if so
            if (!_tournamentManager.IsTournamentIdInDatabase(tournamentId))
            {
                return $"No tournament found with ID: {tournamentId}. Please check the ID and try again.";
            }
            Tournament? tournament = _tournamentManager.GetTournamentById(tournamentId);

            // Can register new teams if Ladder Tournament is running, but cannot register them to Round Robin Tournament or SE/DE Bracket once they have started
            if (_tournamentManager.CanAcceptNewTeams(tournament))
            {
                return $"The tournament '{tournament.Name}' can not accept new teams at this time.";
            }

            // Check if the team name is unique within the tournament
            if (!_teamManager.IsTeamNameUnique(teamName, tournament.Teams))
            {
                return $"The team name '{teamName}' is already taken in the tournament '{tournament.Name}'. Please choose a different name.";
            }

            // Check if member count is valid based on the tournament's team size
            if (!_memberManager.IsMemberCountCorrect(members.Count, tournament.TeamSize))
            {
                return $"The number of members ({members.Count}) does not match the required team size ({tournament.TeamSize}) for the tournament '{tournament.Name}'.";
            }

            // TODO Check if all members are valid and not already registered in the tournament

            // Convert IUser list to Member objects list
            List<Member> convertedMembersList = _memberManager.ConvertMembersListToObjects(members);

            // Create Team object
            Team newTeam = _teamManager.CreateTeam(teamName, tournament.Id, tournament.TeamSize, tournament.TeamSizeFormat, convertedMembersList, tournament.Teams.Count + 1);

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

            // Build advanced return message
            var memberNames = string.Join(", ", convertedMembersList.Select(m => m.DisplayName));
            var sb = new StringBuilder();
            sb.AppendLine($"✅ **Team '{teamName}' has been successfully registered for the tournament '{tournament.Name}'!**");
            sb.AppendLine($"**Team Members ({convertedMembersList.Count}):** {memberNames}");
            sb.AppendLine($"**Team Size Format:** {tournament.TeamSizeFormat}");
            sb.AppendLine($"**Tournament ID:** {tournament.Id}");
            if (!string.IsNullOrWhiteSpace(tournament.Description))
                sb.AppendLine($"**Tournament Description:** {tournament.Description}");

            return sb.ToString();
        }
    }
}
