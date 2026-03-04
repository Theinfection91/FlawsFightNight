using Discord;
using FlawsFightNight.Commands;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Models.Tournaments;
using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.TeamCommands
{
    public class RemoveTeamWinHandler : CommandHandler
    {
        private EmbedFactory _embedFactory;
        private GitBackupService _gitBackupService;
        private TeamService _teamService;
        private TournamentService _tournamentService;
        public RemoveTeamWinHandler(EmbedFactory embedFactory, GitBackupService gitBackupService, TeamService teamService, TournamentService tournamentService) : base("Remove Win")
        {
            _embedFactory = embedFactory;
            _gitBackupService = gitBackupService;
            _teamService = teamService;
            _tournamentService = tournamentService;
        }

        public async Task<Embed> RemoveWinProcess(string teamName, int numberOfWins)
        {
            if (!_teamService.DoesTeamExist(teamName))
            {
                return _embedFactory.ErrorEmbed(Name, $"No team found with the name: {teamName}\n\nPlease check the name and try again.");
            }

            // Grab tournament from team name
            var tournament = _tournamentService.GetTournamentFromTeamName(teamName);

            // Check tournament type
            if (tournament is not NormalLadderTournament)
            {
                // Only ladder tournaments can have wins added manually
                return _embedFactory.ErrorEmbed(Name, $"Wins can only be added manually to teams in Ladder tournaments. The tournament '{tournament.Name}' is a {tournament.Type} tournament.");
            }

            // Check if tournament is running
            if (!tournament.IsRunning)
            {
                return _embedFactory.ErrorEmbed(Name, $"The tournament '{tournament.Name}' is not currently running. Wins can only be added to teams in tournaments that are running.");
            }

            // Validate number of wins
            if (numberOfWins < 1)
            {
                return _embedFactory.ErrorEmbed(Name, $"The number of wins to remove must be at least 1. You provided: {numberOfWins}");
            }

            // Grab team
            var team = tournament.GetTeam(teamName);

            // Check if team has enough wins to remove
            if (team.Wins < numberOfWins)
            {
                return _embedFactory.ErrorEmbed(Name, $"The team '{team.Name}' only has {team.Wins} wins. You cannot remove {numberOfWins} wins.");
            }

            // Remove win(s)
            team.Wins -= numberOfWins;
            team.WinStreak = 0;

            // Save and reload the tournament database
            await _tournamentService.SaveAndReloadTournamentDataFiles(tournament);

            // Backup to git repo
            _gitBackupService.EnqueueBackup();

            return _embedFactory.RemoveTeamWinSuccess(team, tournament, numberOfWins);
        }
    }
}
