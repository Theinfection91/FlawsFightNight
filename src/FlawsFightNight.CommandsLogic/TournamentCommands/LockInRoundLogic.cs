using Discord;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Interfaces;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.TournamentCommands
{
    public class LockInRoundLogic : Logic
    {
        private EmbedManager _embedManager;
        private GitBackupManager _gitBackupManager;
        private TournamentManager _tournamentManager;
        public LockInRoundLogic(EmbedManager embedManager, GitBackupManager gitBackupManager, TournamentManager tournamentManager) : base("Lock In Round")
        {
            _embedManager = embedManager;
            _gitBackupManager = gitBackupManager;
            _tournamentManager = tournamentManager;
        }

        public Embed LockInRoundProcess(string tournamentId)
        {
            // Check if the tournament exists, grab it if so
            if (!_tournamentManager.IsTournamentIdInDatabase(tournamentId))
            {
                return _embedManager.ErrorEmbed(Name, $"No tournament found with ID: {tournamentId}. Please check the ID and try again.");
            }
            var tournament = _tournamentManager.GetNewTournamentById(tournamentId);

            // Check if tournament is running
            if (!tournament.IsRunning)
            {
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' is not currently running.");
            }

            // Check if tournament is IRoundBased
            if (tournament is not IRoundBased roundBasedTournament)
            {
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' is not a round-based tournament and cannot lock in rounds.");
            }
            else
            {
                // Check if the round is already locked in
                if (roundBasedTournament.IsRoundLockedIn)
                {
                    return _embedManager.ErrorEmbed(Name, $"The current round in tournament '{tournament.Name}' is already locked in.");
                }

                // Check if the round can be locked in
                if (!roundBasedTournament.CanLockRound())
                {
                    return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' cannot lock in the current round at this time. Please ensure all matches are complete.");
                }
                // Lock in the round
                roundBasedTournament.LockRound();

                // Save and reload the tournament database
                _tournamentManager.SaveAndReloadTournamentsDatabase();
                
                // Backup to git repo
                _gitBackupManager.CopyAndBackupFilesToGit();

                return _embedManager.LockInRoundSuccess(tournament);
            }
        }
    }
}
