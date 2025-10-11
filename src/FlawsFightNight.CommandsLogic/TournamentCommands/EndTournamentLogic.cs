using Discord;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Models;
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
        private MatchManager _matchManager;
        private TournamentManager _tournamentManager;
        public EndTournamentLogic(EmbedManager embedManager, GitBackupManager gitBackupManager , MatchManager matchManager, TournamentManager tournamentManager) : base("End Tournament")
        {
            _embedManager = embedManager;
            _gitBackupManager = gitBackupManager;
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
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' is not currently running.");
            }

            switch (tournament.Type)
            {
                case TournamentType.RoundRobin:
                    switch (tournament.RoundRobinMatchType)
                    {
                        case RoundRobinMatchType.Open:
                            return EndOpenRoundRobinTournamentProcess(tournament);
                        case RoundRobinMatchType.Normal:
                            return EndNormalRoundRobinTournamentProcess(tournament);
                        default:
                            return _embedManager.ErrorEmbed(Name, "Only Normal and Open Round Robin tournaments are implemented right now. Can not end any other type at this point.");
                    }
                case TournamentType.Ladder:
                    return LadderEndTournament(tournament);
                default:
                    return _embedManager.ErrorEmbed(Name, "Only Round Robin tournaments are supported at this time for ending tournaments.");
            }
        }

        private Embed LadderEndTournament(Tournament tournament)
        {
            // Ladder tournaments can be ended anytime
            tournament.LadderEndTournamentProcess();

            // Replace with better system of grabbing Ladder winner if needed
            // Grab winner (top ranked team)
            string winnerName = tournament.LadderGetRankOneTeam().Name;

            // Save the updated tournament state
            _tournamentManager.SaveAndReloadTournamentsDatabase();

            // Backup to git repo
            _gitBackupManager.CopyAndBackupFilesToGit();

            return _embedManager.EndTournamentSuccessResolver(tournament, winnerName);
        }

        private Embed EndNormalRoundRobinTournamentProcess(Tournament tournament)
        {

            // Check if the tournament can be ended
            if (!tournament.CanEndNormalRoundRobinTournament)
            {
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' cannot be ended at this time. Ensure that all rounds are complete and locked in.");
            }
            //Console.WriteLine($"{_matchManager.IsTieBreakerNeededForFirstPlace(tournament.MatchLog)}");
            // Check if a tie breaker is needed for first place
            if (_matchManager.IsTieBreakerNeededForFirstPlace(tournament.MatchLog))
            {
                (string, string) tieBreakerResult = tournament.TieBreakerRule.ResolveTie(_matchManager.GetTiedTeams(tournament.MatchLog, tournament.IsDoubleRoundRobin), tournament.MatchLog);

                tournament.InitiateEndNormalRoundRobinTournament();

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

                tournament.InitiateEndNormalRoundRobinTournament();

                // Save the updated tournament state
                _tournamentManager.SaveAndReloadTournamentsDatabase();

                // Backup to git repo
                _gitBackupManager.CopyAndBackupFilesToGit();

                return _embedManager.EndTournamentSuccessResolver(tournament, winner);
            }
        }

        private Embed EndOpenRoundRobinTournamentProcess(Tournament tournament)
        {
            if (!tournament.CanEndOpenRoundRobinTournament)
            {
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' cannot be ended at this time. Ensure that all matches have been reported.");
            }

            // Check if a tie breaker is needed for first place
            if (_matchManager.IsTieBreakerNeededForFirstPlace(tournament.MatchLog))
            {
                (string, string) tieBreakerResult = tournament.TieBreakerRule.ResolveTie(_matchManager.GetTiedTeams(tournament.MatchLog, tournament.IsDoubleRoundRobin), tournament.MatchLog);

                tournament.InitiateEndNormalRoundRobinTournament();

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

                tournament.InitiateEndNormalRoundRobinTournament();

                // Save the updated tournament state
                _tournamentManager.SaveAndReloadTournamentsDatabase();

                // Backup to git repo
                _gitBackupManager.CopyAndBackupFilesToGit();

                return _embedManager.EndTournamentSuccessResolver(tournament, winner);
            }
        }
    }
}
