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

namespace FlawsFightNight.Commands.TournamentCommands
{
    public class UnlockTeamsHandler : CommandHandler
    {
        private EmbedFactory _embedFactory;
        private GitBackupService _gitBackupService;
        private TournamentService _tournamentService;
        public UnlockTeamsHandler(EmbedFactory embedFactory, GitBackupService gitBackupService, TournamentService tournamentService) : base("Unlock Teams")
        {
            _embedFactory = embedFactory;
            _gitBackupService = gitBackupService;
            _tournamentService = tournamentService;
        }

        public async Task<Embed> UnlockTeamsProcess(string tournamentId)
        {
            // Check if the tournament exists, grab it if so
            if (!_tournamentService.IsTournamentIdInDatabase(tournamentId))
            {
                return _embedFactory.ErrorEmbed(Name, $"No tournament found with ID: {tournamentId}. Please check the ID and try again.");
            }
            var tournament = _tournamentService.GetTournamentById(tournamentId);

            if (tournament is not ITeamLocking unlockableTournament)
            {
                return _embedFactory.ErrorEmbed(Name, $"The tournament '{tournament.Name}' does not support unlocking teams.");
            }

            if (!unlockableTournament.CanUnlockTeams(out var errorReason))
            {
                return _embedFactory.ErrorEmbed(Name, $"The tournament '{tournament.Name}' cannot be unlocked at this time: {errorReason.Info}");
            }

            // Unlock teams in tournament
            unlockableTournament.UnlockTeams();

            // Save and reload the tournament database
            await _tournamentService.SaveAndReloadTournamentDataFiles(tournament);

            // Backup to git repo
            _gitBackupService.EnqueueBackup();

            return _embedFactory.UnlockTeamsSuccess(tournament);
        }
    }
}
