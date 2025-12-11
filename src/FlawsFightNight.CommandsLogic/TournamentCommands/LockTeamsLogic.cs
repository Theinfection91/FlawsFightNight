using Discord;
using Discord.Interactions;
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
    public class LockTeamsLogic : Logic
    {
        private EmbedManager _embedManager;
        private GitBackupManager _gitBackupManager;
        private TournamentManager _tournamentManager;
        public LockTeamsLogic(EmbedManager embedManager, GitBackupManager gitBackupManager, TournamentManager tournamentManager) : base("Lock Teams")
        {
            _embedManager = embedManager;
            _gitBackupManager = gitBackupManager;
            _tournamentManager = tournamentManager;
        }

        public Embed LockTeamsProcess(string tournamentId)
        {
            // Check if the tournament exists, grab it if so
            if (!_tournamentManager.IsTournamentIdInDatabase(tournamentId))
            {
                return _embedManager.ErrorEmbed(Name, $"No tournament found with ID: {tournamentId}. Please check the ID and try again.");
            }
            var tournament = _tournamentManager.GetTournamentById(tournamentId);

            // Check if tournament is ITeamLocking type
            if (tournament is not ITeamLocking lockableTournament)
            {
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' does not support locking teams.");
            }

            // Check if teams can be locked
            if (!lockableTournament.CanLockTeams(out var errorReason))
            {
                // TODO Implement new ErrorReason object with embed for better error handling
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' cannot be locked at this time: {errorReason.Info}");
            }

            // Lock the teams in the tournament
            lockableTournament.LockTeams();

            // Save and reload the tournament database
            _tournamentManager.SaveAndReloadTournamentDataFiles(tournament);

            // Backup to git repo
            _gitBackupManager.CopyAndBackupFilesToGit();

            // Return success embed
            return _embedManager.LockTeamsSuccess(tournament);
        }
    }
}
