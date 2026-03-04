using Discord;
using FlawsFightNight.Commands;
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
    public class UnlockRoundLogic : CommandHandler
    {
        private EmbedFactory _embedFactory;
        private GitBackupService _gitBackupService;
        private TournamentService _tournamentService;
        public UnlockRoundLogic(EmbedFactory embedFactory, GitBackupService gitBackupService, TournamentService tournamentService) : base("Unlock Round")
        {
            _embedFactory = embedFactory;
            _gitBackupService = gitBackupService;
            _tournamentService = tournamentService;
        }

        public async Task<Embed> UnlockRoundProcess(string tournamentId)
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

            // Check if the tournament is IRoundBased
            if (tournament is not IRoundBased roundBasedTournament)
            {
                return _embedFactory.ErrorEmbed(Name, $"The tournament '{tournament.Name}' is not a round-based tournament and does not support unlocking rounds.");
            }
            else
            {
                // Check if the round is already unlocked
                if (!roundBasedTournament.IsRoundLockedIn)
                {
                    return _embedFactory.ErrorEmbed(Name, $"The round in the tournament '{tournament.Name}' is already unlocked and ready to be locked in.");
                }

                // Check if the round can be unlocked
                if (!roundBasedTournament.CanUnlockRound())
                {
                    return _embedFactory.ErrorEmbed(Name, $"The current round in tournament '{tournament.Name}' cannot be unlocked at this time.");
                }

                // Unlock the round
                roundBasedTournament.UnlockRound();

                // Save and reload the tournament database
                await _tournamentService.SaveAndReloadTournamentDataFiles(tournament);

                // Backup to git repo
                _gitBackupService.EnqueueBackup();

                return _embedFactory.UnlockRoundSuccess(tournament);
            }
        }
    }
}
