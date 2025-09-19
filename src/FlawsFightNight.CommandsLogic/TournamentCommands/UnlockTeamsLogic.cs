using Discord;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.TournamentCommands
{
    public class UnlockTeamsLogic : Logic
    {
        private EmbedManager _embedManager;
        private GitBackupManager _gitBackupManager;
        private TournamentManager _tournamentManager;
        public UnlockTeamsLogic(EmbedManager embedManager, GitBackupManager gitBackupManager, TournamentManager tournamentManager) : base("Unlock Teams")
        {
            _embedManager = embedManager;
            _gitBackupManager = gitBackupManager;
            _tournamentManager = tournamentManager;
        }

        public Embed UnlockTeamsProcess(string tournamentId)
        {
            // Check if the tournament exists, grab it if so
            if (!_tournamentManager.IsTournamentIdInDatabase(tournamentId))
            {
                return _embedManager.ErrorEmbed(Name, $"No tournament found with ID: {tournamentId}. Please check the ID and try again.");
            }
            var tournament = _tournamentManager.GetTournamentById(tournamentId);

            // Handle different tournament types
            switch (tournament.Type)
            {
                case TournamentType.Ladder:
                    return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' is a Ladder tournament and does not require unlocking teams.");
                case TournamentType.RoundRobin:
                    return RoundRobinUnlockTeamsProcess(tournament);
                default:
                    return _embedManager.ErrorEmbed(Name, "Tournament type not supported for unlocking teams yet.");
            }
        }

        private Embed RoundRobinUnlockTeamsProcess(Tournament tournament)
        {          
            // Check if tournament is already running
            if (tournament.IsRunning)
            {
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' is currently running. Teams cannot be unlocked while the tournament is active.");
            }

            // Check if the tournament is already unlocked
            if (!tournament.IsTeamsLocked)
            {
                return _embedManager.ErrorEmbed(Name, $"The teams in the tournament '{tournament.Name}' are already unlocked.");
            }

            // Check if tournament can be unlocked
            if (!tournament.CanTeamsBeUnlocked)
            {
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' cannot be unlocked at this time.");
            }

            // Unlock the teams in the tournament
            _tournamentManager.UnlockTeamsInTournament(tournament);

            // Save and reload the tournament database
            _tournamentManager.SaveAndReloadTournamentsDatabase();

            // Backup to git repo
            _gitBackupManager.CopyAndBackupFilesToGit();

            return _embedManager.UnlockTeamsSuccess(tournament);
        }
    }
}
