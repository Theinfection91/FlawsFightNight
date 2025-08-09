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
        private TournamentManager _tournamentManager;
        public NextRoundLogic(TournamentManager tournamentManager) : base("Next Round")
        {
            _tournamentManager = tournamentManager;
        }

        public string NextRoundProcess(string tournamentId)
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
                return $"The round for tournament '{tournament.Name}' is not complete. Please ensure all matches are reported before locking in the round.";
            }

            // Check if the round is already locked in
            if (!tournament.IsRoundLockedIn)
            {
                return $"The round for tournament '{tournament.Name}' is not locked in.";
            }

            if (tournament.CanEndTournament)
            {
                return $"The tournament '{tournament.Name}' is ready to end so you cannot go to the next round. Please use the appropriate command to end it.";
            }

            // Advance to the next round
            _tournamentManager.NextRoundResolver(tournament);
            _tournamentManager.SaveAndReloadTournamentsDatabase();
            return $"The tournament '{tournament.Name}' has advanced to round {tournament.CurrentRound}.";
        }
    }
}
