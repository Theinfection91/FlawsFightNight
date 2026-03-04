using Discord;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Models.Tournaments;
using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.TeamCommands
{
    public class AddTeamWinLogic : Logic
    {
        private EmbedFactory _embedManager;
        private GitBackupService _gitBackupManager;
        private TeamService _teamManager;
        private TournamentService _tournamentManager;

        public AddTeamWinLogic(EmbedFactory embedManager, GitBackupService gitBackupManager, TeamService teamManager, TournamentService tournamentManager) : base("Add Win")
        {
            _embedManager = embedManager;
            _gitBackupManager = gitBackupManager;
            _teamManager = teamManager;
            _tournamentManager = tournamentManager;
        }

        public async Task<Embed> AddTeamWinProcess(string teamName, int numberOfWins)
        {
            if (!_teamManager.DoesTeamExist(teamName))
            {
                return _embedManager.ErrorEmbed(Name, $"No team found with the name: {teamName}\n\nPlease check the name and try again.");
            }
            // Grab tournament from team name
            var tournament = _tournamentManager.GetTournamentFromTeamName(teamName);

            // Check tournament type
            if (tournament is not NormalLadderTournament)
            {
                // Only ladder tournaments can have wins added manually
                return _embedManager.ErrorEmbed(Name, $"Wins can only be added manually to teams in Normal Ladder tournaments. The tournament '{tournament.Name}' is a {tournament.Type} tournament.");
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
            var team = tournament.GetTeam(teamName);

            // Add win(s)
            team.Wins += numberOfWins;
            team.WinStreak += numberOfWins;
            // Reset lose streak
            team.LoseStreak = 0;

            // Save and reload the tournament database
            await _tournamentManager.SaveAndReloadTournamentDataFiles(tournament);

            // Backup to git repo
            _gitBackupManager.EnqueueBackup();

            return _embedManager.AddTeamWinSuccess(team, tournament, numberOfWins);
        }
    }
}
