using Discord;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Interfaces;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.TournamentCommands
{
    public class UnlockTeamsLogic : Logic
    {
        private EmbedFactory _embedManager;
        private GitBackupService _gitBackupManager;
        private TournamentService _tournamentManager;
        public UnlockTeamsLogic(EmbedFactory embedManager, GitBackupService gitBackupManager, TournamentService tournamentManager) : base("Unlock Teams")
        {
            _embedManager = embedManager;
            _gitBackupManager = gitBackupManager;
            _tournamentManager = tournamentManager;
        }

        public async Task<Embed> UnlockTeamsProcess(string tournamentId)
        {
            // Check if the tournament exists, grab it if so
            if (!_tournamentManager.IsTournamentIdInDatabase(tournamentId))
            {
                return _embedManager.ErrorEmbed(Name, $"No tournament found with ID: {tournamentId}. Please check the ID and try again.");
            }
            var tournament = _tournamentManager.GetTournamentById(tournamentId);

            if (tournament is not ITeamLocking unlockableTournament)
            {
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' does not support unlocking teams.");
            }

            if (!unlockableTournament.CanUnlockTeams(out var errorReason))
            {
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' cannot be unlocked at this time: {errorReason.Info}");
            }

            // Unlock teams in tournament
            unlockableTournament.UnlockTeams();

            // Save and reload the tournament database
            await _tournamentManager.SaveAndReloadTournamentDataFiles(tournament);

            // Backup to git repo
            _gitBackupManager.EnqueueBackup();

            return _embedManager.UnlockTeamsSuccess(tournament);
        }
    }
}
