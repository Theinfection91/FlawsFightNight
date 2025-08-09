using FlawsFightNight.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.TournamentCommands
{
    public class StartTournamentLogic : Logic
    {
        private MatchManager _matchManager;
        private TournamentManager _tournamentManager;

        public StartTournamentLogic(MatchManager matchManager, TournamentManager tournamentManager) : base("Start Tournament")
        {
            _matchManager = matchManager;
            _tournamentManager = tournamentManager;
        }

        public string StartTournamentProcess(string tournamentId)
        {
            // Check if the tournament exists, grab it if so
            if (!_tournamentManager.IsTournamentIdInDatabase(tournamentId))
            {
                return $"No tournament found with ID: {tournamentId}. Please check the ID and try again.";
            }
            var tournament = _tournamentManager.GetTournamentById(tournamentId);
            // Check if the tournament is already running
            if (tournament.IsRunning)
            {
                return $"The tournament '{tournament.Name}' is already running.";
            }
            // Check if teams are locked
            if (!tournament.IsTeamsLocked)
            {
                return $"The teams in the tournament '{tournament.Name}' are not locked. Please lock the teams before starting the tournament.";
            }
            // Start the tournament
            _matchManager.BuildMatchScheduleResolver(tournament);
            tournament.CurrentRound = 1;
            tournament.IsRunning = true;

            // Save and reload the tournament database
            _tournamentManager.SaveAndReloadTournamentsDatabase();
            return $"The tournament '{tournament.Name}' has been successfully started.";
        }
    }
}
