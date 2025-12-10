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

        public Embed DeleteTournamentProcess(string tournamentId)
        {
            // Grab tournament, modal should have ensured it exists
            var tournament = _tournamentManager.GetTournamentById(tournamentId);

            if (!tournament.CanDelete())
            {
                return _embedManager.ErrorEmbed(Name, "Tournament cannot be deleted in its current state. Make sure it is not running and all teams are unlocked if applicable.");
            }

            // Delete the tournament
            _tournamentManager.DeleteTournament(tournament.Id);

            // Save and reload the database
            //_tournamentManager.SaveAndReloadTournamentsDatabase();
            //_tournamentManager.SaveTournament(tournament);
            _tournamentManager.LoadTournamentDataFiles();

            // Backup to git repo
            _gitBackupManager.CopyAndBackupFilesToGit();

            // Return success embed
            return _embedManager.DeleteTournamentSuccess(tournament);
        }
    }
}
