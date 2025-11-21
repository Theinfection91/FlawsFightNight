using Discord;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Interfaces;
using FlawsFightNight.Core.Models;
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
            var tournament = _tournamentManager.GetNewTournamentById(tournamentId);

            // Check if tournament is running
            if (!tournament.IsRunning)
            {
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' is not currently running.");
            }

            // Check if the tournament is IRoundBased
            if (tournament is not IRoundBased roundBasedTournament)
            {
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' is not a round-based tournament and does not support unlocking rounds.");
            }
            else
            {
                // Check if the round is already unlocked
                if (!roundBasedTournament.IsRoundLockedIn)
                {
                    return _embedManager.ErrorEmbed(Name, $"The round in the tournament '{tournament.Name}' is already unlocked and ready to be locked in.");
                }

                // Check if the round can be unlocked
                if (!roundBasedTournament.CanUnlockRound())
                {
                    return _embedManager.ErrorEmbed(Name, $"The current round in tournament '{tournament.Name}' cannot be unlocked at this time.");
                }

                // Unlock the round
                roundBasedTournament.UnlockRound();

                // Save and reload the tournament database
                _tournamentManager.SaveAndReloadTournamentsDatabase();

                // Backup to git repo
                _gitBackupManager.CopyAndBackupFilesToGit();

                return _embedManager.UnlockRoundSuccess(tournament);
            }
        }+
    }
}
