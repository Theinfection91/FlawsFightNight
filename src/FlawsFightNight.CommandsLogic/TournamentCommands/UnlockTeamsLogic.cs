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
        private TournamentManager _tournamentManager;
        public UnlockTeamsLogic(TournamentManager tournamentManager) : base("Unlock Teams")
        {
            _tournamentManager = tournamentManager;
        }

        public string UnlockTeamsProcess(string tournamentId)
        {
            // Check if the tournament exists, grab it if so
            if (!_tournamentManager.IsTournamentIdInDatabase(tournamentId))
            {
                return $"No tournament found with ID: {tournamentId}. Please check the ID and try again.";
            }
            var tournament = _tournamentManager.GetTournamentById(tournamentId);

            // Check if tournament is already running
            if (tournament.IsRunning)
            {
                return $"The tournament '{tournament.Name}' is currently running. Teams cannot be unlocked while the tournament is active.";
            }

            // Check if the tournament is already unlocked
            if (!tournament.IsTeamsLocked)
            {
                return $"The teams in the tournament '{tournament.Name}' are already unlocked.";
            }

            // Check if tournament can be unlocked
            if (!tournament.CanTeamsBeUnlocked)
            {
                return $"The tournament '{tournament.Name}' cannot be unlocked at this time.";
            }

            // Unlock the teams in the tournament
            _tournamentManager.UnlockTeamsInTournament(tournament);

            // Save and reload the tournament database
            _tournamentManager.SaveAndReloadTournamentsDatabase();
            return $"The teams in the tournament '{tournament.Name}' have been successfully unlocked.";
        }
    }
}
