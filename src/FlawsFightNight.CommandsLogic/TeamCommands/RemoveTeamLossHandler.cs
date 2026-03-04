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
    public class RemoveTeamLossHandler : CommandHandler
    {
        private EmbedFactory _embedFactory;
        private GitBackupService _gitBackupService;
        private TeamService _teamService;
        private TournamentService _tournamentService;

        public RemoveTeamLossHandler(EmbedFactory embedFactory, GitBackupService gitBackupService, TeamService teamService, TournamentService tournamentService) : base("Remove Loss")
        {
            _embedFactory = embedFactory;
            _gitBackupService = gitBackupService;
            _teamService = teamService;
            _tournamentService = tournamentService;
        }

        public async Task<Embed> RemoveLossProcess(string teamName, int numberOfLosses)
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
                // Only ladder tournaments can have losses removed manually
                return _embedFactory.ErrorEmbed(Name, $"Losses can only be removed manually from teams in Ladder tournaments. The tournament '{tournament.Name}' is a {tournament.Type} tournament.");
            }

            // Check if tournament is running
            if (!tournament.IsRunning)
            {
                return _embedFactory.ErrorEmbed(Name, $"The tournament '{tournament.Name}' is not currently running. Losses can only be removed from teams in tournaments that are running.");
            }

            // Validate number of losses
            if (numberOfLosses < 1)
            {
                return _embedFactory.ErrorEmbed(Name, $"The number of losses to remove must be at least 1. You provided: {numberOfLosses}");
            }

            // Grab team
            var team = tournament.GetTeam(teamName);

            // Check if team has enough losses to remove
            if (team.Losses < numberOfLosses)
            {
                return _embedFactory.ErrorEmbed(Name, $"The team '{team.Name}' only has {team.Losses} losses. You cannot remove {numberOfLosses} losses.");
            }

            // Remove loss(es)
            team.Losses -= numberOfLosses;
            team.LoseStreak = 0;

            // Save and reload the tournament database
            await _tournamentService.SaveAndReloadTournamentDataFiles(tournament);

            // Backup to git repo
            _gitBackupService.EnqueueBackup();

            return _embedFactory.RemoveTeamLossSuccess(team, tournament, numberOfLosses);
        }
    }
}
