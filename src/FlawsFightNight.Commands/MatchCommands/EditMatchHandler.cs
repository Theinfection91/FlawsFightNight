using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Core.Models.Tournaments;
using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.MatchCommands
{
    public class EditMatchHandler : CommandHandler
    {
        private EmbedFactory _embedFactory;
        private GitBackupService _gitBackupService;
        private MatchService _matchService;
        private TeamService _teamService;
        private TournamentService _tournamentService;
        public EditMatchHandler(EmbedFactory embedFactory, GitBackupService gitBackupService, MatchService matchService, TeamService teamService, TournamentService tournamentService) : base("Edit Match")
        {
            _embedFactory = embedFactory;
            _gitBackupService = gitBackupService;
            _matchService = matchService;
            _teamService = teamService;
            _tournamentService = tournamentService;
        }

        public async Task<Embed> EditMatchProcess(string matchId, string winningTeamName, int winningTeamScore, int losingTeamScore)
        {
            // Basic validation of score inputs
            if (winningTeamScore < 0 || losingTeamScore < 0)
            {
                return _embedFactory.ErrorEmbed(Name, "Scores cannot be negative. Please provide valid scores and try again.");
            }
            if (winningTeamScore < losingTeamScore)
            {
                return _embedFactory.ErrorEmbed(Name, "The winning team's score must be greater than or equal to the losing team's score. Please check the scores and try again.");
            }

            // Check if the match exists in the database (To Play or Previous)
            if (!_matchService.IsMatchIdInDatabase(matchId))
            {
                return _embedFactory.ErrorEmbed(Name, $"No match found with ID: {matchId}. Please check the ID and try again.");
            }
            // Grab the tournament associated with the match if it does exist
            var tournament = _tournamentService.GetTournamentFromMatchId(matchId);

            // Check if tournament is running
            if (!tournament.IsRunning)
            {
                return _embedFactory.ErrorEmbed(Name, "The tournament is not currently running. You cannot edit matches in a round robin tournament that is not running.");
            }

            // Currently only Round Robin tournaments support match editing
            if (tournament is not NormalRoundRobinTournament or OpenRoundRobinTournament)
            {
                return _embedFactory.ErrorEmbed(Name, $"Editing matches is not supported for {tournament.Type} tournaments at this time.");
            }

            // For Normal RR, check if round is locked in
            if ((tournament is NormalRoundRobinTournament normalRR) && normalRR.IsRoundLockedIn)
            {
                return _embedFactory.ErrorEmbed(Name, "The current round is locked. You cannot edit previous matches unless a round is unlocked.");
            }

            // Check if match has been played, can only edit played matches in RR
            if (!_matchService.HasMatchBeenPlayed(tournament, matchId))
            {
                return _embedFactory.ErrorEmbed(Name, $"The match with ID: {matchId} has not been played yet. You can only edit matches that have been played in a Round Robin Tournament.");
            }

            // Grab post match
            var postMatch = _matchService.GetPostMatchByIdInTournament(tournament, matchId);

            // Check if it was a bye week match, no need to edit those
            if (postMatch.WasByeMatch)
            {
                return _embedFactory.ErrorEmbed(Name, $"The match with ID: {matchId} was a Bye week match. There is no need to edit Bye week matches.");
            }

            // Check if given team name is in the match
            if (!_matchService.IsGivenTeamNameInPostMatch(winningTeamName, postMatch))
            {
                return _embedFactory.ErrorEmbed(Name, $"The team '{winningTeamName}' is not part of the match with ID: {matchId}. Please check the team name and try again.");
            }

            // For Normal RR, check if match is in current round being played
            //if (tournament is NormalRoundRobinTournament && !_matchService.IsPostMatchInCurrentRound(tournament, postMatch.Id))
            //{
            //    return _embedFactory.ErrorEmbed(Name, $"The match with ID: {matchId} is not in the current round being played. You can only edit matches from the current round.");
            //}

            // Roll back old results
            _teamService.EditMatchRollback(tournament, postMatch);

            // Update post match with new results
            postMatch.UpdateResultsProcess(winningTeamName, winningTeamScore, losingTeamScore);

            // Apply new results
            _teamService.EditMatchApply(tournament, postMatch);

            // Recalculate streaks for the two teams involved
            _matchService.RecalculateAllWinLossStreaks(tournament);

            // Re-rank and save
            tournament.AdjustRanks();

            // Save the database
            await _tournamentService.SaveAndReloadTournamentDataFiles(tournament);

            // Backup to git
            _gitBackupService.EnqueueBackup();

            return _embedFactory.RoundRobinEditMatchSuccess(tournament, postMatch);
        }
    }
}
