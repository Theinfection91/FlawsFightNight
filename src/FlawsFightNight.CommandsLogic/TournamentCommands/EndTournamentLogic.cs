using Discord;
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
        private EmbedManager _embedManager;
        private LiveViewManager _liveViewManager;
        private MatchManager _matchManager;
        private TournamentManager _tournamentManager;
        public EndTournamentLogic(EmbedManager embedManager, MatchManager matchManager, TournamentManager tournamentManager) : base("End Tournament")
        {
            _embedManager = embedManager;
            _matchManager = matchManager;
            _tournamentManager = tournamentManager;
        }

        public Embed EndTournamentProcess(string tournamentId)
        {
            var tournament = _tournamentManager.GetTournamentById(tournamentId);

            // Check if tournament is already running
            if (!tournament.IsRunning)
            {
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' is not currently running. Rounds cannot be unlocked while the tournament is inactive.");
            }

            // Check if the tournament can be ended
            if (!tournament.CanEndTournament)
            {
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' cannot be ended at this time. Ensure that all rounds are complete and locked in.");
            }

            // TODO Tiebreaker Logic
            if (_matchManager.IsTieBreakerNeeded(tournament.MatchLog))
            {
                //Console.WriteLine($"A tiebreaker is needed for the tournament '{tournament.Name}'.");
                //Console.WriteLine($"Number of teams tied: {_matchManager.GetNumberOfTeamsTied(tournament.MatchLog)}");
                //Console.WriteLine($"Teams tied: {string.Join(", ", _matchManager.GetTiedTeams(tournament.MatchLog, tournament.IsDoubleRoundRobin))}");
                //Console.WriteLine($"*True winner: {tournament.TieBreakerRule.ResolveTie(_matchManager.GetTiedTeams(tournament.MatchLog, tournament.IsDoubleRoundRobin), tournament.MatchLog)}");

                (string, string) tieBreakerResult = tournament.TieBreakerRule.ResolveTie(_matchManager.GetTiedTeams(tournament.MatchLog, tournament.IsDoubleRoundRobin), tournament.MatchLog);

                return _embedManager.RoundRobinEndTournamentWithTieBreakerSuccess(tournament, tieBreakerResult);
            }
            else
            {
                // TODO Will change this to a more robust solution later
                string winner = _liveViewManager.GetRoundRobinStandings(tournament).Entries.FirstOrDefault().TeamName;

                // Save the updated tournament state
                _tournamentManager.SaveAndReloadTournamentsDatabase();

                return _embedManager.EndTournamentSuccessResolver(tournament, winner);
            }   
        }
    }
}
