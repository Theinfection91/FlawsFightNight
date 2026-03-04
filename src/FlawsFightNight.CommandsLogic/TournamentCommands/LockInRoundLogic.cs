using Discord;
using FlawsFightNight.Commands;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Interfaces;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.TournamentCommands
{
    public class LockInRoundLogic : CommandHandler
    {
        private EmbedFactory _embedFactory;
        private GitBackupService _gitBackupService;
        private TournamentService _tournamentService;
        public LockInRoundLogic(EmbedFactory embedFactory, GitBackupService gitBackupService, TournamentService tournamentService) : base("Lock In Round")
        {
            _embedFactory = embedFactory;
            _gitBackupService = gitBackupService;
            _tournamentService = tournamentService;
        }

        public async Task<Embed> LockInRoundProcess(string tournamentId)
        {
            // Check if the tournament exists, grab it if so
            if (!_tournamentService.IsTournamentIdInDatabase(tournamentId))
            {
                return _embedFactory.ErrorEmbed(Name, $"No tournament found with ID: {tournamentId}. Please check the ID and try again.");
            }
            var tournament = _tournamentService.GetTournamentById(tournamentId);

            // Check if tournament is running
            if (!tournament.IsRunning)
            {
                return _embedFactory.ErrorEmbed(Name, $"The tournament '{tournament.Name}' is not currently running.");
            }

            // Check if tournament is IRoundBased
            if (tournament is not IRoundBased roundBasedTournament)
            {
                return _embedFactory.ErrorEmbed(Name, $"The tournament '{tournament.Name}' is not a round-based tournament and cannot lock in rounds.");
            }
            else
            {
                // Check if the round is already locked in
                if (roundBasedTournament.IsRoundLockedIn)
                {
                    return _embedFactory.ErrorEmbed(Name, $"The current round in tournament '{tournament.Name}' is already locked in.");
                }

                // Check if the round can be locked in
                if (!roundBasedTournament.CanLockRound())
                {
                    return _embedFactory.ErrorEmbed(Name, $"The tournament '{tournament.Name}' cannot lock in the current round at this time. Please ensure all matches are complete.");
                }
                // Lock in the round
                roundBasedTournament.LockRound();

                // Save and reload the tournament database
                await _tournamentService.SaveAndReloadTournamentDataFiles(tournament);

                // Backup to git repo
                _gitBackupService.EnqueueBackup();

                return _embedFactory.LockInRoundSuccess(tournament);
            }
        }
    }
}
