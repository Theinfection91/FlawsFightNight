using Discord;
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
        private EmbedManager _embedManager;
        private MatchManager _matchManager;
        private TournamentManager _tournamentManager;

        public StartTournamentLogic(EmbedManager embedManager, MatchManager matchManager, TournamentManager tournamentManager) : base("Start Tournament")
        {
            _embedManager = embedManager;
            _matchManager = matchManager;
            _tournamentManager = tournamentManager;
        }

        public Embed StartTournamentProcess(string tournamentId)
        {
            // Check if the tournament exists, grab it if so
            if (!_tournamentManager.IsTournamentIdInDatabase(tournamentId))
            {
                _embedManager.ErrorEmbed(Name, $"No tournament found with ID: {tournamentId}. Please check the ID and try again.");
            }
            var tournament = _tournamentManager.GetTournamentById(tournamentId);
            // Check if the tournament is already running
            if (tournament.IsRunning)
            {
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' is already running.");
            }
            // Check if teams are locked
            if (!tournament.IsTeamsLocked)
            {
                return _embedManager.ErrorEmbed(Name, $"The teams in the tournament '{tournament.Name}' are not locked. Please lock the teams before starting the tournament.");
            }
            // Start the tournament
            _matchManager.BuildMatchScheduleResolver(tournament);
            tournament.InitiateStartTournament();

            // Save and reload the tournament database
            _tournamentManager.SaveAndReloadTournamentsDatabase();
            
            // Return Embed with tournament information
            return _embedManager.StartTournamentSuccessResolver(tournament);
        }
    }
}
