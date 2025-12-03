using Discord;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Interfaces;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Core.Models.Tournaments;
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
        private MatchManager _matchManager;
        private TournamentManager _tournamentManager;
        public EndTournamentLogic(EmbedManager embedManager, GitBackupManager gitBackupManager, MatchManager matchManager, TournamentManager tournamentManager) : base("End Tournament")
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

            if (!tournament.CanEnd(out var errorReason))
            {
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' cannot be ended at this time: {errorReason.Info}");
            }

            // Handle Normal and Open Round Robin Tournaments
            if (tournament is ITieBreakerRankSystem tbTournament)
            {
                if (_matchManager.IsTieBreakerNeededForFirstPlace(tournament.MatchLog))
                {
                    // Resolve tie breaker info
                    string tieBreakerInfo = tbTournament.TieBreakerRule.ResolveTie(_matchManager.GetTiedTeams(tournament.MatchLog), tournament.MatchLog).Item1;

                    // End the tournament
                    tournament.End();

                    // Save the updated tournament state
                    _tournamentManager.SaveAndReloadTournamentsDatabase();

                    // Backup to git repo
                    _gitBackupManager.CopyAndBackupFilesToGit();

                    return _embedManager.RoundRobinEndTournamentSuccess(tournament, true, tieBreakerInfo);
                }
                else
                {
                    // End the tournament, no tie breaker needed
                    tournament.End();

                    // Save the updated tournament state
                    _tournamentManager.SaveAndReloadTournamentsDatabase();

                    // Backup to git repo
                    _gitBackupManager.CopyAndBackupFilesToGit();

                    return _embedManager.RoundRobinEndTournamentSuccess(tournament);
                }
            }

            // Handle Normal Ladder Tournament
            if (tournament is NormalLadderTournament ladderTournament)
            {
                // End the tournament, normal ladder tournaments can be ended as long as they are running
                ladderTournament.End();

                // Save the updated tournament state
                _tournamentManager.SaveAndReloadTournamentsDatabase();

                // Backup to git repo
                _gitBackupManager.CopyAndBackupFilesToGit();

                return _embedManager.NormalLadderEndTournamentSuccess(tournament);
            }

            // Handle DSR Tournament
            if (tournament is DSRLadderTournament dsrTournament)
            {
                // End the tournament, DSR tournaments can be ended as long as they are running
                dsrTournament.End();

                // Save the updated tournament state
                _tournamentManager.SaveAndReloadTournamentsDatabase();

                // Backup to git repo
                _gitBackupManager.CopyAndBackupFilesToGit();

                return _embedManager.DSRLadderEndTournamentSuccess(tournament);
            }

            return _embedManager.ErrorEmbed(Name, "An error occurred while trying to end the tournament. Please try again later.");
        }
    }
}
