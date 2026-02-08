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
                    // Resolve tie breaker info and get the winner
                    var tiedTeams = _matchManager.GetTiedTeams(tournament.MatchLog);
                    var (tieBreakerInfo, winnerTeamName) = tbTournament.TieBreakerRule.ResolveTie(tiedTeams, tournament.MatchLog);

                    // Apply tie-breaker rankings to the tied teams
                    ApplyTieBreakerRankings(tournament, tiedTeams, winnerTeamName);

                    // End the tournament
                    tournament.End();

                    // Save the updated tournament state
                    _tournamentManager.SaveAndReloadTournamentDataFiles(tournament);

                    // Backup to git repo
                    _gitBackupManager.CopyAndBackupFilesToGit();

                    return _embedManager.RoundRobinEndTournamentSuccess(tournament, true, tieBreakerInfo);
                }
                else
                {
                    // End the tournament, no tie breaker needed
                    tournament.End();

                    // Save the updated tournament state
                    _tournamentManager.SaveAndReloadTournamentDataFiles(tournament);

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
                _tournamentManager.SaveAndReloadTournamentDataFiles(tournament);

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
                _tournamentManager.SaveAndReloadTournamentDataFiles(tournament);

                // Backup to git repo
                _gitBackupManager.CopyAndBackupFilesToGit();

                return _embedManager.DSRLadderEndTournamentSuccess(tournament);
            }

            return _embedManager.ErrorEmbed(Name, "An error occurred while trying to end the tournament. Please try again later.");
        }

        /// <summary>
        /// Applies tie-breaker rankings to tied teams. The winner gets rank 1, 
        /// and other tied teams are ranked sequentially based on their original order.
        /// Non-tied teams keep their existing ranks.
        /// </summary>
        private void ApplyTieBreakerRankings(Tournament tournament, List<string> tiedTeams, string winnerTeamName)
        {
            // Find the minimum rank among tied teams (should be 1 for first place tie)
            var tiedTeamObjects = tournament.Teams.Where(t => tiedTeams.Contains(t.Name, StringComparer.OrdinalIgnoreCase)).ToList();
            int minRank = tiedTeamObjects.Min(t => t.Rank);

            // Get the winner team
            var winnerTeam = tiedTeamObjects.FirstOrDefault(t => t.Name.Equals(winnerTeamName, StringComparison.OrdinalIgnoreCase));
            if (winnerTeam == null)
            {
                return; // Winner not found, abort
            }

            // Assign ranks: winner gets minRank, others get minRank + 1, minRank + 2, etc.
            winnerTeam.Rank = minRank;

            // Get the other tied teams (excluding the winner)
            var otherTiedTeams = tiedTeamObjects.Where(t => !t.Name.Equals(winnerTeamName, StringComparison.OrdinalIgnoreCase)).ToList();

            // Assign sequential ranks to remaining tied teams
            int currentRank = minRank + 1;
            foreach (var team in otherTiedTeams)
            {
                team.Rank = currentRank;
                currentRank++;
            }

            // Adjust ranks of teams that were below the tied teams
            var teamsToShift = tournament.Teams
                .Where(t => !tiedTeams.Contains(t.Name, StringComparer.OrdinalIgnoreCase) && t.Rank >= minRank)
                .ToList();

            foreach (var team in teamsToShift)
            {
                team.Rank += tiedTeams.Count - 1; // Shift by number of tied teams minus 1 (since one already had minRank)
            }
        }
    }
}
