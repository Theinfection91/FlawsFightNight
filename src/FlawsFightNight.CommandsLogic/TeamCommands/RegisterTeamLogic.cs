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
        private ConfigManager _configManager;
        private EmbedManager _embedManager;
        private GitBackupManager _gitBackupManager;
        private MemberManager _memberManager;
        private TournamentManager _tournamentManager;
        private TeamManager _teamManager;

        public RegisterTeamLogic(ConfigManager configManager, EmbedManager embedManager, GitBackupManager gitBackupManager, MemberManager memberManager, TournamentManager tournamentManager, TeamManager teamManager) : base("Register Team")
        {
            _configManager = configManager;
            _embedManager = embedManager;
            _gitBackupManager = gitBackupManager;
            _memberManager = memberManager;
            _tournamentManager = tournamentManager;
            _teamManager = teamManager;
        }

        public Embed RegisterTeamProcess(SocketInteractionContext context, string teamName, string tournamentId, List<IUser> members)
        {
            // Cannot try to report Bye as a team
            if (teamName.Equals("Bye", StringComparison.OrdinalIgnoreCase))
            {
                return _embedManager.ErrorEmbed(Name, $"Teams are not allowed to have any variation of the singular name 'Bye' for data purposes. \n\nUser input: {teamName}");
            }

            // Check if the tournament exists, grab it if so
            if (!_tournamentManager.IsTournamentIdInDatabase(tournamentId))
            {
                return _embedManager.ErrorEmbed(Name, $"No tournament found with ID: {tournamentId}. Please check the ID and try again.");
            }
            Tournament? tournament = _tournamentManager.GetTournamentById(tournamentId);

            // Can register new teams if Ladder Tournament is running, but cannot register them to Round Robin Tournament or SE/DE Bracket once they have started
            if (_tournamentManager.CanAcceptNewTeams(tournament))
            {
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' can not accept new teams at this time. Check if teams are locked.");
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

            // Convert IUser list to Member objects list
            List<Member> convertedMembersList = _memberManager.ConvertMembersListToObjects(members);

            // Check if all members are valid and not already registered in the tournament (If they are, check if they are in debug admin list)
            foreach (Member member in convertedMembersList)
            {
                if (_memberManager.IsMemberRegisteredInTournament(member.DiscordId, tournament) && !_configManager.IsDiscordIdInDebugAdminList(member.DiscordId))
                {
                    return _embedManager.ErrorEmbed(Name, $"Member '{member.DisplayName}' (ID: {member.DiscordId}) is already registered in the tournament '{tournament.Name}'. Each member can only be part of one team per tournament.");
                }
            }

            // Create Team object
            Team newTeam = _teamManager.CreateTeam(teamName, convertedMembersList, tournament.Teams.Count + 1);

            // Add the new team to the tournament
            _tournamentManager.AddTeamToTournament(newTeam, tournament.Id);

            // Check if tournament can be locked after adding the team
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

            // Update the tournament standings
            _tournamentManager.UpdateTournamentStandings(tournament);

            Console.WriteLine($"After updated method: {tournament.RoundRobinStandings.Entries.Count}");

            // Save and reload the tournament database
            //_tournamentManager.SaveTournamentsDatabase();

            // Backup to git repo
            _gitBackupManager.CopyAndBackupFilesToGit();

            return _embedManager.TeamRegistrationSuccess(newTeam, tournament);
        }
    }
}
