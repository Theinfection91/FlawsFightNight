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
    public class LockInRoundLogic : Logic
    {
        private EmbedManager _embedManager;
        private GitBackupManager _gitBackupManager;
        private MatchManager _matchManager;
        private TournamentManager _tournamentManager;
        public LockInRoundLogic(EmbedManager embedManager, GitBackupManager gitBackupManager, MatchManager matchManager, TournamentManager tournamentManager) : base("Lock In Round")
        {
            _embedManager = embedManager;
            _gitBackupManager = gitBackupManager;
            _matchManager = matchManager;
            _tournamentManager = tournamentManager;
        }

        public Embed LockInRoundProcess(string tournamentId)
        {
            // Check if the tournament exists, grab it if so
            if (!_tournamentManager.IsTournamentIdInDatabase(tournamentId))
            {
                return _embedManager.ErrorEmbed(Name, $"No tournament found with ID: {tournamentId}. Please check the ID and try again.");
            }
            var tournament = _tournamentManager.GetTournamentById(tournamentId);

            // Check if tournament is running
            if (!tournament.IsRunning)
            {
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' is not currently running.");
            }

            // Handle different tournament types
            switch (tournament.Type)
            {
                case TournamentType.Ladder:
                    return _embedManager.ErrorEmbed(Name, "Ladder tournaments do not have rounds to lock in.");
                case TournamentType.RoundRobin:
                    return RoundRobinLockInRoundProcess(tournament);
                default:
                    return _embedManager.ErrorEmbed(Name, "Tournament type not supported for locking in rounds yet.");
            }
        }

        private Embed RoundRobinLockInRoundProcess(Tournament tournament)
        {
            if (!tournament.RoundRobinMatchType.Equals(RoundRobinMatchType.Normal))
            {
                return _embedManager.ErrorEmbed(Name, $"Only Normal Round Robin tournaments support locking in rounds at this time.");
            }

            // Check if the round is complete
            if (!tournament.IsRoundComplete)
            {
                if (_matchManager.HasByeMatchRemaining(tournament))
                {
                    return _embedManager.ErrorEmbed(Name, $"The round for tournament '{tournament.Name}' is not complete due to a bye match remaining. Please ensure all matches are reported before locking in the round.");
                }
                return _embedManager.ErrorEmbed(Name, $"The round for tournament '{tournament.Name}' is not complete. Please ensure all matches are reported before locking in the round.");
            }

            // Check if the round is already locked in
            if (tournament.IsRoundLockedIn)
            {
                return _embedManager.ErrorEmbed(Name, $"The round for tournament '{tournament.Name}' is already locked in.");
            }

            // Lock in the round
            tournament.IsRoundLockedIn = true;

            // Save and reload the tournament database
            _tournamentManager.SaveAndReloadTournamentsDatabase();

            // Backup to git repo
            _gitBackupManager.CopyAndBackupFilesToGit();

            return _embedManager.LockInRoundSuccess(tournament);
        }
    }
}
