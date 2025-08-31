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
        private GitBackupManager _gitBackupManager;
        private LiveViewManager _liveViewManager;
        private MatchManager _matchManager;
        private TournamentManager _tournamentManager;
        public EndTournamentLogic(EmbedManager embedManager, GitBackupManager gitBackupManager , LiveViewManager liveViewManager, MatchManager matchManager, TournamentManager tournamentManager) : base("End Tournament")
        {
            _embedManager = embedManager;
            _gitBackupManager = gitBackupManager;
            _liveViewManager = liveViewManager;
            _matchManager = matchManager;
            _tournamentManager = tournamentManager;
        }

        public Embed EndTournamentProcess(string tournamentId)
        {
            var tournament = _tournamentManager.GetTournamentById(tournamentId);

            // Check if tournament is already running
            if (!tournament.IsRunning)
            {
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' is not currently running.");
            }

            // Check if the tournament can be ended
            if (!tournament.CanEndTournament)
            {
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' cannot be ended at this time. Ensure that all rounds are complete and locked in.");
            }
            //Console.WriteLine($"{_matchManager.IsTieBreakerNeededForFirstPlace(tournament.MatchLog)}");
            // TODO Tiebreaker Logic
            if (_matchManager.IsTieBreakerNeededForFirstPlace(tournament.MatchLog))
            {
                (string, string) tieBreakerResult = tournament.TieBreakerRule.ResolveTie(_matchManager.GetTiedTeams(tournament.MatchLog, tournament.IsDoubleRoundRobin), tournament.MatchLog);

                tournament.InitiateEndTournament();

                // Save the updated tournament state
                _tournamentManager.SaveAndReloadTournamentsDatabase();

                return _embedManager.RoundRobinEndTournamentWithTieBreakerSuccess(tournament, tieBreakerResult);
            }
            else
            {
                // No tie breaker needed, grab winner with most wins
                string winner = _liveViewManager.GetRoundRobinStandings(tournament).Entries.FirstOrDefault().TeamName;

                tournament.InitiateEndTournament();

                // Save the updated tournament state
                _tournamentManager.SaveAndReloadTournamentsDatabase();

                // Backup to git repo
                _gitBackupManager.CopyAndBackupFilesToGit();

                return _embedManager.EndTournamentSuccessResolver(tournament, winner);
            }   
        }
    }
}
