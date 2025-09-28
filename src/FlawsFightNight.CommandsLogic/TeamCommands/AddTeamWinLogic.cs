using Discord;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.TeamCommands
{
    public class AddTeamWinLogic : Logic
    {
        private EmbedManager _embedManager;
        private GitBackupManager _gitBackupManager;
        private TeamManager _teamManager;
        private TournamentManager _tournamentManager;

        public AddTeamWinLogic(EmbedManager embedManager, GitBackupManager gitBackupManager, TeamManager teamManager, TournamentManager tournamentManager) : base("Add Win")
        {
            _embedManager = embedManager;
            _gitBackupManager = gitBackupManager;
            _teamManager = teamManager;
            _tournamentManager = tournamentManager;
        }

        public Embed AddTeamWinProcess(string teamName, int numberOfWins)
        {
            if (!_teamManager.DoesTeamExist(teamName))
            {
                return _embedManager.ErrorEmbed(Name, $"No team found with the name: {teamName}\n\nPlease check the name and try again.");
            }
            // Grab tournament from team name
            var tournament = _tournamentManager.GetTournamentFromTeamName(teamName);

            // Check tournament type
            if (!tournament.Type.Equals(TournamentType.Ladder))
            {
                // Only ladder tournaments can have wins added manually
                return _embedManager.ErrorEmbed(Name, $"Wins can only be added manually to teams in Ladder tournaments. The tournament '{tournament.Name}' is a {tournament.Type} tournament.");
            }

            // Check if tournament is running
            if (!tournament.IsRunning)
            {
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' is not currently running. Wins can only be added to teams in tournaments that are running.");
            }

            // Validate number of wins
            if (numberOfWins < 1)
            {
                return _embedManager.ErrorEmbed(Name, $"The number of wins to add must be at least 1. You provided: {numberOfWins}");
            }

            // Grab team
            var team = _teamManager.GetTeamByName(teamName);

            // Add win(s)
            // TODO Expand this later on for logging purposes
            team.Wins += numberOfWins;
            team.WinStreak += numberOfWins;
            // Reset lose streak
            team.LoseStreak = 0;

            // Save and reload the tournament database
            _tournamentManager.SaveAndReloadTournamentsDatabase();

            // Backup to git repo
            _gitBackupManager.CopyAndBackupFilesToGit();

            return _embedManager.AddTeamWinSuccess(team, tournament, numberOfWins);
        }
    }
}
