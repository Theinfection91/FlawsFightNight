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
    public class NextRoundHandler : CommandHandler
    {
        private EmbedFactory _embedFactory;
        private GitBackupService _gitBackupService;
        private TournamentService _tournamentService;
        public NextRoundHandler(EmbedFactory embedFactory, GitBackupService gitBackupService, TournamentService tournamentService) : base("Next Round")
        {
            _embedFactory = embedFactory;
            _gitBackupService = gitBackupService;
            _tournamentService = tournamentService;
        }

        public async Task<Embed> NextRoundProcess(string tournamentId)
        {
            // Check if the tournament exists, grab it if so
            if (!_tournamentService.IsTournamentIdInDatabase(tournamentId))
            {
                return _embedFactory.ErrorEmbed(Name, $"No tournament found with ID: {tournamentId}. Please check the ID and try again.");
            }
            var tournament = _tournamentService.GetTournamentById(tournamentId);

            if (!tournament.IsRunning)
            {
                return _embedFactory.ErrorEmbed(Name, $"The tournament '{tournament.Name}' is not currently running.");
            }

            // Check if the tournament is IRoundBased
            if (tournament is not IRoundBased roundBasedTournament)
            {
                return _embedFactory.ErrorEmbed(Name, $"The tournament '{tournament.Name}' is not a round-based tournament and does not support round mechanics.");
            }
            else
            {
                if (!roundBasedTournament.CanAdvanceRound())
                {
                    return _embedFactory.ErrorEmbed(Name, $"The tournament '{tournament.Name}' cannot advance to the next round at this time. Ensure all matches are completed and there is another round to advance to.");
                }

                // Advance to next round
                roundBasedTournament.AdvanceRound();

                // Save and reload the tournament database
                await _tournamentService.SaveAndReloadTournamentDataFiles(tournament);

                // Backup to git repo
                _gitBackupService.EnqueueBackup();

                return _embedFactory.NextRoundSuccess(tournament, roundBasedTournament.CurrentRound);
            }
        }
    }
}
