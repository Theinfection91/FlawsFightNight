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
    public class RemoveTeamLossLogic : Logic
    {
        private EmbedManager _embedManager;
        private GitBackupManager _gitBackupManager;
        private TeamManager _teamManager;
        private TournamentManager _tournamentManager;
        public RemoveTeamLossLogic(EmbedManager embedManager, GitBackupManager gitBackupManager, TeamManager teamManager, TournamentManager tournamentManager) : base("Remove Loss")
        {
            _embedManager = embedManager;
            _gitBackupManager = gitBackupManager;
            _teamManager = teamManager;
            _tournamentManager = tournamentManager;
        }

        public Embed RemoveLossProcess(string teamName, int numberOfLosses)
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
                // Only ladder tournaments can have losses removed manually
                return _embedManager.ErrorEmbed(Name, $"Losses can only be removed manually from teams in Ladder tournaments. The tournament '{tournament.Name}' is a {tournament.Type} tournament.");
            }

            // Check if tournament is running
            if (!tournament.IsRunning)
            {
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' is not currently running. Losses can only be removed from teams in tournaments that are running.");
            }

            // Validate number of losses
            if (numberOfLosses < 1)
            {
                return _embedManager.ErrorEmbed(Name, $"The number of losses to remove must be at least 1. You provided: {numberOfLosses}");
            }

            // Grab team
            var team = _teamManager.GetTeamByName(teamName);

            // Check if team has enough losses to remove
            if (team.Losses < numberOfLosses)
            {
                return _embedManager.ErrorEmbed(Name, $"The team '{team.Name}' only has {team.Losses} losses. You cannot remove {numberOfLosses} losses.");
            }

            // Remove loss(es)
            team.Losses -= numberOfLosses;
            team.LoseStreak = 0;

            // Save and reload the tournament database
            _tournamentManager.SaveAndReloadTournamentsDatabase();

            // Backup to git repo
            _gitBackupManager.CopyAndBackupFilesToGit();

            return _embedManager.RemoveTeamLossSuccess(team, tournament, numberOfLosses);
        }
    }
}
