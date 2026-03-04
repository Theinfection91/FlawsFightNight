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
    public class RemoveMatchesChannelLogic : CommandHandler
    {
        private EmbedFactory _embedFactory;
        private GitBackupService _gitBackupService;
        private TournamentService _tournamentService;

        public RemoveMatchesChannelLogic(EmbedFactory embedFactory, GitBackupService gitBackupService, TournamentService tournamentService) : base("Remove Matches Channel")
        {
            _embedFactory = embedFactory;
            _gitBackupService = gitBackupService;
            _tournamentService = tournamentService;
        }
        public async Task<Embed> RemoveMatchesChannelProcess(string tournamentId)
        {
            // Check if the tournament exists, grab it if so
            if (!_tournamentService.IsTournamentIdInDatabase(tournamentId))
            {
                return _embedFactory.ErrorEmbed(Name, $"No tournament found with ID: {tournamentId}. Please check the ID and try again.");
            }
            var tournament = _tournamentService.GetTournamentById(tournamentId);

            // Check if a matches channel is set
            if (tournament.MatchesChannelId == 0)
            {
                return _embedFactory.ErrorEmbed(Name, $"Tournament {tournament.Name} ({tournament.Id}) does not have a matches channel set.");
            }

            // Remove the matches channel
            tournament.MatchesChannelId = 0;
            tournament.MatchesMessageId = 0;

            // Save and reload the tournaments database
            await _tournamentService.SaveAndReloadTournamentDataFiles(tournament);

            // Backup to git repo
            _gitBackupService.EnqueueBackup();

            return _embedFactory.RemoveMatchesChannelSuccess(tournament);
        }
    }
}
