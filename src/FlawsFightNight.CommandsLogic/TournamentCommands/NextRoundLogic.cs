using Discord;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.TournamentCommands
{
    public class NextRoundLogic : Logic
    {
        private EmbedManager _embedManager;
        private GitBackupManager _gitBackupManager;
        private TournamentManager _tournamentManager;
        public NextRoundLogic(EmbedManager embedManager, GitBackupManager gitBackupManager, TournamentManager tournamentManager) : base("Next Round")
        {
            _embedManager = embedManager;
            _gitBackupManager = gitBackupManager;
            _tournamentManager = tournamentManager;
        }

        public Embed NextRoundProcess(string tournamentId)
        {
            // Check if the tournament exists, grab it if so
            if (!_tournamentManager.IsTournamentIdInDatabase(tournamentId))
            {
                return _embedManager.ErrorEmbed(Name, $"No tournament found with ID: {tournamentId}. Please check the ID and try again.");
            }
            Tournament? tournament = _tournamentManager.GetTournamentById(tournamentId);

            // Check if the round is complete
            if (!tournament.IsRoundComplete)
            {
                return _embedManager.ErrorEmbed(Name, $"The round for tournament '{tournament.Name}' is not complete. Please ensure all matches are reported before locking in the round.");
            }

            // Check if the round is already locked in
            if (!tournament.IsRoundLockedIn)
            {
                return _embedManager.ErrorEmbed(Name, $"The round for tournament '{tournament.Name}' is not locked in.");
            }

            if (tournament.CanEndNormalRoundRobinTournament)
            {
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' is ready to end so you cannot go to the next round. Please use the appropriate command to end it.");
            }

            // Advance to the next round
            _tournamentManager.NextRoundResolver(tournament);

            // Save and reload the tournament database
            _tournamentManager.SaveAndReloadTournamentsDatabase();

            // Backup to git repo
            _gitBackupManager.CopyAndBackupFilesToGit();

            return _embedManager.NextRoundSuccess(tournament);
        }
    }
}
