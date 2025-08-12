using FlawsFightNight.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.TournamentCommands
{
    public class EndTournamentLogic : Logic
    {
        private MatchManager _matchManager;
        private TournamentManager _tournamentManager;
        public EndTournamentLogic(MatchManager matchManager, TournamentManager tournamentManager) : base("End Tournament")
        {
            _matchManager = matchManager;
            _tournamentManager = tournamentManager;
        }

        public string EndTournamentProcess(string tournamentId)
        {
            // Check if the tournament exists, grab it if so
            if (!_tournamentManager.IsTournamentIdInDatabase(tournamentId))
            {
                return $"No tournament found with ID: {tournamentId}. Please check the ID and try again.";
            }

            var tournament = _tournamentManager.GetTournamentById(tournamentId);

            // Check if tournament is already running
            if (!tournament.IsRunning)
            {
                return $"The tournament '{tournament.Name}' is not currently running. Rounds cannot be unlocked while the tournament is inactive.";
            }

            // Check if the tournament can be ended
            if (!tournament.CanEndTournament)
            {
                return $"The tournament '{tournament.Name}' cannot be ended at this time. Ensure that all rounds are complete and locked in.";
            }

            // TODO Tiebreaker Logic
            if (_matchManager.IsTieBreakerNeeded(tournament.MatchLog))
            {
                Console.WriteLine($"A tiebreaker is needed for the tournament '{tournament.Name}'.");
                Console.WriteLine($"Number of teams tied: {_matchManager.GetNumberOfTeamsTied(tournament.MatchLog)}");
                Console.WriteLine($"Teams tied: {string.Join(", ", _matchManager.GetTiedTeams(tournament.MatchLog))}");
                Console.WriteLine($"*True winner: {tournament.TieBreakerRule.ResolveTie(_matchManager.GetTiedTeams(tournament.MatchLog), tournament.MatchLog)}");
            }



            // Save the updated tournament state
            _tournamentManager.SaveAndReloadTournamentsDatabase();

            return "TODO";
        }
    }
}
