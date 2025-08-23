using Discord;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.MatchCommands
{
    public class EditMatchLogic : Logic
    {
        private EmbedManager _embedManager;
        private MatchManager _matchManager;
        private TournamentManager _tournamentManager;
        public EditMatchLogic(EmbedManager embedManager, MatchManager matchManager, TournamentManager tournamentManager) : base("Edit Match")
        {
            _embedManager = embedManager;
            _matchManager = matchManager;
            _tournamentManager = tournamentManager;
        }

        public Embed EditMatchProcess(string matchId, string winningTeamName, int winningTeamScore, int losingTeamScore)
        {
            // Check if the match exists in the database (To Play or Previous)
            if (!_matchManager.IsMatchIdInDatabase(matchId))
            {
                return _embedManager.ErrorEmbed(Name, $"No match found with ID: {matchId}. Please check the ID and try again.");
            }
            // Grab the tournament associated with the match if it does exist
            var tournament = _tournamentManager.GetTournamentFromMatchId(matchId);

            // Send through switch resolver based on tournament type
            switch (tournament.Type)
            {
                case Core.Enums.TournamentType.RoundRobin:
                    return RoundRobinEditMatchProcess(tournament, matchId, winningTeamName, winningTeamScore, losingTeamScore);
                default:
                    break;
            }
            return _embedManager.ErrorEmbed(Name, $"Editing matches is not supported for {tournament.Type} tournaments at this time.");
        }

        private Embed RoundRobinEditMatchProcess(Tournament tournament, string matchId, string winningTeamName, int winningTeamScore, int losingTeamScore)
        {
            // Check if tournament is running
            if (!tournament.IsRunning)
            {
                return _embedManager.ErrorEmbed(Name, "The tournament is not currently running. You cannot edit matches in a round robin tournament that is not running.");
            }

            // Check if round is unlocked
            if (tournament.IsRoundLockedIn)
            {
                return _embedManager.ErrorEmbed(Name, "The current round is locked. You cannot edit previous matches unless a round is unlocked.");
            }

            // Check if match has been played, can only edit played matches in RR
            if (!_matchManager.HasMatchBeenPlayed(tournament, matchId))
            {
                return _embedManager.ErrorEmbed(Name, $"The match with ID: {matchId} has not been played yet. You can only edit matches that have been played in a Round Robin Tournament.");
            }

            if (winningTeamScore < 0 || losingTeamScore < 0)
            {
                return _embedManager.ErrorEmbed(Name, "Scores cannot be negative. Please provide valid scores and try again.");
            }

            if (winningTeamScore < losingTeamScore)
            {
                return _embedManager.ErrorEmbed(Name, "The winning team's score must be greater than or equal to the losing team's score. Please check the scores and try again.");
            }

            // Grab post match
            var postMatch = _matchManager.GetPostMatchByIdInTournament(tournament, matchId);

            // Check if post match to edit is within current round being played, cannot edit matches from previous rounds that were locked in
            if (!_matchManager.IsPostMatchInCurrentRound(tournament, postMatch.Id))
            {
                return _embedManager.ErrorEmbed(Name, $"The match with ID: {matchId} is not in the current round being played. You can only edit matches from the current round.");
            }

            // Check if given team name is in the match
            if (!_matchManager.IsGivenTeamNameInPostMatch(winningTeamName, postMatch))
            {
                return _embedManager.ErrorEmbed(Name, $"The team '{winningTeamName}' is not part of the match with ID: {matchId}. Please check the team name and try again.");
            }

            // Check if it was a bye week match, no need to edit those
            if (postMatch.WasByeMatch)
                {
                return _embedManager.ErrorEmbed(Name, $"The match with ID: {matchId} was a Bye week match. There is no need to edit Bye week matches.");
            }

            // Update the match results
            postMatch.UpdateResultsProcess(winningTeamName, winningTeamScore, losingTeamScore);

            // Save and reload the tournament database
            _tournamentManager.SaveAndReloadTournamentsDatabase();

            return _embedManager.RoundRobinEditMatchSuccess(tournament, postMatch);
        }
    }
}
