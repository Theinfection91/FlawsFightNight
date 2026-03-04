using Discord;
using FlawsFightNight.Commands;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Interfaces;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Core.Models.Tournaments;
using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.TournamentCommands
{
    public class DeleteTournamentHandler : CommandHandler
    {
        private EmbedFactory _embedFactory;
        private GitBackupService _gitBackupService;
        private TournamentService _tournamentService;

        public DeleteTournamentHandler(EmbedFactory embedFactory, GitBackupService gitBackupService, TournamentService tournamentService) : base("Delete Tournament")
        {
            _embedFactory = embedFactory;
            _gitBackupService = gitBackupService;
            _tournamentService = tournamentService;
        }

        public async Task<Embed> DeleteTournamentProcess(string tournamentId)
        {
            // Grab tournament, modal should have ensured it exists
            var tournament = _tournamentService.GetTournamentById(tournamentId);

            if (!tournament.CanDelete(out var errorReason))
            {
                return _embedFactory.ErrorEmbed(Name, $"The tournament {tournament.Name} cannot be deleted at this time: {errorReason.Info}");
            }

            // Delete the tournament
            await _tournamentService.DeleteTournament(tournament.Id);

            // Load tournament data files again, now that one is deleted
            await _tournamentService.LoadTournamentDataFiles();

            // Backup to git repo
            _gitBackupService.EnqueueBackup();

            // Return success embed
            return _embedFactory.DeleteTournamentSuccess(tournament);
        }
    }
}
