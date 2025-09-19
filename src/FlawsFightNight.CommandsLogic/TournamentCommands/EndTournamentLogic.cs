using Discord;
using FlawsFightNight.Managers;
using FlawsFightNight.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlawsFightNight.Core.Models;

namespace FlawsFightNight.CommandsLogic.TournamentCommands
{
    public class EndTournamentLogic : Logic
    {
        private EmbedManager _embedManager;
        private GitBackupManager _gitBackupManager;
        private LiveViewManager _liveViewManager;
        private MatchManager _matchManager;
        private TournamentManager _tournamentManager;
        public EndTournamentLogic(EmbedManager embedManager, GitBackupManager gitBackupManager, LiveViewManager liveViewManager, MatchManager matchManager, TournamentManager tournamentManager) : base("End Tournament")
        {
            _embedManager = embedManager;
            _gitBackupManager = gitBackupManager;
            _liveViewManager = liveViewManager;
            _matchManager = matchManager;
            _tournamentManager = tournamentManager;
        }

        public Embed EndTournamentProcess(string tournamentId)
        {
            // Grab tournament, modal should have ensured it exists
            var tournament = _tournamentManager.GetTournamentById(tournamentId);

            // Check if tournament is already running
            if (!tournament.IsRunning)
            {
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' is not currently running. Cannot end a tournament that has not started.");
            }

            // Handle different tournament types
            switch (tournament.Type)
            {
                case TournamentType.Ladder:
                    return LadderEndTournament(tournament);
                case TournamentType.RoundRobin:
                    return RoundRobinEndTournament(tournament);
                default:
                    return _embedManager.ErrorEmbed(Name, "Tournament type not supported for ending yet.");
            }
        }

        private Embed LadderEndTournament(Tournament tournament)
        {
            // Ladder tournaments can be ended anytime
            tournament.LadderEndTournamentProcess();

            // Grab winner (top ranked team)
            string winnerName = tournament.LadderGetRankOneTeam().Name;

            // Save the updated tournament state
            _tournamentManager.SaveAndReloadTournamentsDatabase();

            // Backup to git repo
            _gitBackupManager.CopyAndBackupFilesToGit();

            return _embedManager.EndTournamentSuccessResolver(tournament, winnerName);
        }

        private Embed RoundRobinEndTournament(Tournament tournament)
        {
            // Check if the tournament can be ended
            if (!tournament.CanEndRoundRobinTournament)
            {
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' cannot be ended at this time. Ensure that all rounds are complete and locked in.");
            }
            //Console.WriteLine($"{_matchManager.IsTieBreakerNeededForFirstPlace(tournament.MatchLog)}");
            // Check if a tie breaker is needed for first place
            if (_matchManager.IsTieBreakerNeededForFirstPlace(tournament.MatchLog))
            {
                (string, string) tieBreakerResult = tournament.TieBreakerRule.ResolveTie(_matchManager.GetTiedTeams(tournament.MatchLog, tournament.IsDoubleRoundRobin), tournament.MatchLog);

                tournament.RoundRobinEndTournamentProcess();

                // Save the updated tournament state
                _tournamentManager.SaveAndReloadTournamentsDatabase();

                // Backup to git repo
                _gitBackupManager.CopyAndBackupFilesToGit();

                return _embedManager.RoundRobinEndTournamentWithTieBreakerSuccess(tournament, tieBreakerResult);
            }
            else
            {
                // No tie breaker needed, grab winner rank at #1
                string winner = tournament.Teams.OrderBy(t => t.Rank).First().Name;

                tournament.RoundRobinEndTournamentProcess();

                // Save the updated tournament state
                _tournamentManager.SaveAndReloadTournamentsDatabase();

                // Backup to git repo
                _gitBackupManager.CopyAndBackupFilesToGit();

                return _embedManager.EndTournamentSuccessResolver(tournament, winner);
            }
        }
    }
}
