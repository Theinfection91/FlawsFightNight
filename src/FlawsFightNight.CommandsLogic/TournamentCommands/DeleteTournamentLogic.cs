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
    public class DeleteTournamentLogic : Logic
    {
        private EmbedManager _embedManager;
        private GitBackupManager _gitBackupManager;
        private TournamentManager _tournamentManager;

        public DeleteTournamentLogic(EmbedManager embedManager, GitBackupManager gitBackupManager, TournamentManager tournamentManager) : base("Delete Tournament")
        {
            _embedManager = embedManager;
            _gitBackupManager = gitBackupManager;
            _tournamentManager = tournamentManager;
        }

        public Embed DeleteTournamentProcess(string tournamentId)
        {
            // Grab tournament, modal should have ensured it exists
            var tournament = _tournamentManager.GetTournamentById(tournamentId);

            // Check if tournament is running
            if (tournament.IsRunning)
            {
                return _embedManager.ErrorEmbed(Name, "Cannot delete a tournament that is currently running. End the tournament and try again.");
            }

            // Handle different tournament types
            switch (tournament.Type)
            {
                case TournamentType.Ladder:
                    return LadderDeleteTournamentProcess(tournament);
                case TournamentType.RoundRobin:
                    return RoundRobinDeleteTournament(tournament);
                default:
                    return _embedManager.ErrorEmbed(Name, "Tournament type not supported for deletion yet.");
            }
            return _embedManager.ErrorEmbed(Name, "Tournament type not supported for deletion yet.");
        }

        private Embed LadderDeleteTournamentProcess(Tournament tournament)
        {
            // As long as the tournament is not running, we can delete it. 

            // Delete the tournament, this will also save and reload the database
            _tournamentManager.DeleteTournament(tournament.Id);

            // Backup to git repo
            _gitBackupManager.CopyAndBackupFilesToGit();

            return _embedManager.DeleteTournamentSuccess(tournament);
        }

        private Embed RoundRobinDeleteTournament(Tournament tournament)
        {
            if (!tournament.IsRunning && tournament.IsTeamsLocked)
            {
                return _embedManager.ErrorEmbed(Name, "Even though the tournament is not running, the teams are still locked. Unlock teams first if you want to delete this tournament. Once started, it cannot be deleted until it is ended.");
            }

            // Delete the tournament, this will also save and reload the database
            _tournamentManager.DeleteTournament(tournament.Id);

            // Backup to git repo
            _gitBackupManager.CopyAndBackupFilesToGit();

            return _embedManager.DeleteTournamentSuccess(tournament);
        }
    }
}
