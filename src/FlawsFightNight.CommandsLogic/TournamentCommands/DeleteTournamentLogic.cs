using Discord;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Interfaces;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Core.Models.Tournaments;
using FlawsFightNight.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.TournamentCommands
{
    public class DeleteTournamentLogic : Logic
    {
        private EmbedManager _embedManager;
        private GitBackupManager _gitBackupManager;
        private TournamentManager _tournamentManager;

        public DeleteTournamentLogic(EmbedManager embedManager, GitBackupManager gitBackupManager, TournamentManager tournamentManager) : base("Delete Tournament")
        {
            _embedManager = embedManager;
            _gitBackupManager = gitBackupManager;
            _tournamentManager = tournamentManager;
        }

        public async Task<Embed> DeleteTournamentProcess(string tournamentId)
        {
            // Grab tournament, modal should have ensured it exists
            var tournament = _tournamentManager.GetTournamentById(tournamentId);

            if (!tournament.CanDelete(out var errorReason))
            {
                return _embedManager.ErrorEmbed(Name, $"The tournament {tournament.Name} cannot be deleted at this time: {errorReason.Info}");
            }

            // Delete the tournament
            await _tournamentManager.DeleteTournament(tournament.Id);

            // Load tournament data files again, now that one is deleted
            await _tournamentManager.LoadTournamentDataFiles();

            // Backup to git repo
            _gitBackupManager.EnqueueBackup();

            // Return success embed
            return _embedManager.DeleteTournamentSuccess(tournament);
        }
    }
}
