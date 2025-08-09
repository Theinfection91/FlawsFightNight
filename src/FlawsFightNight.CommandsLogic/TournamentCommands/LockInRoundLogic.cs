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
        private MatchManager _matchManager;
        private TournamentManager _tournamentManager;
        public LockInRoundLogic(MatchManager matchManager, TournamentManager tournamentManager) : base("Lock In Round")
        {
            _matchManager = matchManager;
            _tournamentManager = tournamentManager;
        }
        public string LockInRoundProcess(string tournamentId)
        {
            // Check if the tournament exists, grab it if so
            if (!_tournamentManager.IsTournamentIdInDatabase(tournamentId))
            {
                return $"No tournament found with ID: {tournamentId}. Please check the ID and try again.";
            }
            Tournament? tournament = _tournamentManager.GetTournamentById(tournamentId);

            // Check if the round is complete
            if (!tournament.IsRoundComplete)
            {
                if (_matchManager.HasByeMatchRemaining(tournament))
                {
                    return $"The round for tournament '{tournament.Name}' is not complete due to a bye match remaining. Please ensure all matches are reported before locking in the round.";
                }
                return $"The round for tournament '{tournament.Name}' is not complete. Please ensure all matches are reported before locking in the round.";
            }

            // Check if the round is already locked in
            if (tournament.IsRoundLockedIn)
            {
                return $"The round for tournament '{tournament.Name}' is already locked in.";
            }

            // Lock in the round
            tournament.IsRoundLockedIn = true;
            // Save and reload the tournament database
            _tournamentManager.SaveAndReloadTournamentsDatabase();
            return $"The round for tournament '{tournament.Name}' has been successfully locked in. Teams can now advance to the next round using /tournament next-round.";
        }
    }
}
