using Discord;
using FlawsFightNight.Commands;
using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.SettingsCommands
{
    public class RemoveTeamsChannelHandler : CommandHandler
    {
        private EmbedFactory _embedFactory;
        private GitBackupService _gitBackupService;
        private TournamentService _tournamentService;

        public RemoveTeamsChannelHandler(EmbedFactory embedFactory, GitBackupService gitBackupService, TournamentService tournamentService) : base("Remove Teams Channel")
        {
            _embedFactory = embedFactory;
            _gitBackupService = gitBackupService;
            _tournamentService = tournamentService;
        }

        public async Task<Embed> RemoveTeamsChannelProcess(string tournamentId)
        {
            // Check if the tournament exists, grab it if so
            if (!_tournamentService.IsTournamentIdInDatabase(tournamentId))
            {
                return _embedFactory.ErrorEmbed(Name, $"No tournament found with ID: {tournamentId}. Please check the ID and try again.");
            }
            var tournament = _tournamentService.GetTournamentById(tournamentId);
            // Check if a teams channel is set
            if (tournament.TeamsChannelId == 0)
            {
                return _embedFactory.ErrorEmbed(Name, $"Tournament {tournament.Name} ({tournament.Id}) does not have a teams channel set.");
            }
            // Remove the teams channel
            tournament.TeamsChannelId = 0;
            tournament.TeamsMessageId = 0;

            // Save and reload the tournaments database
            await _tournamentService.SaveAndReloadTournamentDataFiles(tournament);

            // Backup to git repo
            _gitBackupService.EnqueueBackup();

            return _embedFactory.RemoveTeamsChannelSuccess(tournament);
        }
    }
}
