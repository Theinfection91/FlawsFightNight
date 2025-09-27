using Discord;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.TournamentCommands
{
    public class UnlockRoundLogic : Logic
    {
        private EmbedManager _embedManager;
        private GitBackupManager _gitBackupManager;
        private TournamentManager _tournamentManager;
        public UnlockRoundLogic(EmbedManager embedManager, GitBackupManager gitBackupManager, TournamentManager tournamentManager) : base("Unlock Round")
        {
            _embedManager = embedManager;
            _gitBackupManager = gitBackupManager;
            _tournamentManager = tournamentManager;
        }

        public Embed UnlockRoundProcess(string tournamentId)
        {
            // Check if the tournament exists, grab it if so
            if (!_tournamentManager.IsTournamentIdInDatabase(tournamentId))
            {
                return _embedManager.ErrorEmbed(Name, $"No tournament found with ID: {tournamentId}. Please check the ID and try again.");
            }

            var tournament = _tournamentManager.GetTournamentById(tournamentId);

            if (!tournament.RoundRobinMatchType.Equals(RoundRobinMatchType.Normal))
            {
                return _embedManager.ErrorEmbed(Name, $"Only Normal Round Robin tournaments support unlocking rounds at this time.");
            }

            // Check if tournament is already running
            if (!tournament.IsRunning)
            {
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' is not currently running. Rounds cannot be unlocked while the tournament is inactive.");
            }

            if (!tournament.IsRoundComplete)
            {
                return _embedManager.ErrorEmbed(Name, $"The current round for tournament '{tournament.Name}' is not complete, cannot unlock round that was never locked in.");
            }

            // Check if the tournament is already unlocked
            if (!tournament.IsRoundLockedIn)
            {
                // If the round is not locked in, it means it is already unlocked
                return _embedManager.ErrorEmbed(Name, $"The round in the tournament '{tournament.Name}' is already unlocked and ready to be locked in.");
            }

            // Unlock the rounds in the tournament
            tournament.IsRoundLockedIn = false;

            // Save and reload the tournament database
            _tournamentManager.SaveAndReloadTournamentsDatabase();

            // Backup to git repo
            _gitBackupManager.CopyAndBackupFilesToGit();

            return _embedManager.UnlockRoundSuccess(tournament);
        }
    }
}
