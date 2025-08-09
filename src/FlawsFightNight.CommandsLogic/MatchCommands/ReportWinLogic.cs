using Discord.Interactions;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.MatchCommands
{
    public class ReportWinLogic : Logic
    {
        private MatchManager _matchManager;
        private TeamManager _teamManager;
        private TournamentManager _tournamentManager;
        public ReportWinLogic(MatchManager matchManager, TeamManager teamManager, TournamentManager tournamentManager) : base("Report Win")
        { 
            _matchManager = matchManager;
            _teamManager = teamManager;
            _tournamentManager = tournamentManager;
        }

        public string ReportWinProcess(SocketInteractionContext context, string winningTeamName, int winningTeamScore, int losingTeamScore)
        {
            if (losingTeamScore > winningTeamScore)
            {
                return "The losing team score cannot be greater than the winning team score.";
            }

            // Check if team exists across all tournaments
            if (!_teamManager.DoesTeamExist(winningTeamName))
            {
                return $"The team '{winningTeamName}' does not exist or is not registered in any tournament.";
            }

            // Grab the tournament associated with the match
            Tournament tournament = _tournamentManager.GetTournamentFromTeamName(winningTeamName);

            if (tournament.IsRoundComplete) return "The tournament is showing the round has been marked as complete.";

            // Check if the team has a match scheduled
            if (!_matchManager.IsMatchMadeForTeam(tournament, winningTeamName))
            {
                return $"The team '{winningTeamName}' does not have a match scheduled.";
            }

            // Grab the match associated with report
            Match match = _matchManager.GetMatchByTeamName(tournament, winningTeamName);

            // Grab the winning team
            Team? winningTeam = _teamManager.GetTeamByName(tournament, winningTeamName);

            // Grab the losing team
            Team? losingTeam = _teamManager.GetTeamByName(_matchManager.GetLosingTeamName(match, winningTeamName));

            // Check if invoker is on winning team
            if (!_teamManager.IsDiscordIdOnTeam(winningTeam, context.User.Id))
            {
                return $"You are not a member of the team '{winningTeamName}' and cannot report a win for them.";
            }

            switch (tournament.Type)
            {
                case TournamentType.Ladder:
                    // Handle Ladder specific logic

                    break;
                case TournamentType.RoundRobin:
                    // Handle Round Robin specific logic
                    return HandleRoundRobinWin(tournament, match, winningTeam, losingTeam, winningTeamScore, losingTeamScore);
                case TournamentType.SingleElimination:
                case TournamentType.DoubleElimination:
                    // Handle Single/Double Elimination specific logic

                    break;
            }
            return "Win reported successfully.";
        }

        private string HandleRoundRobinWin(Tournament tournament, Match match, Team winningTeam, Team losingTeam, int winningTeamScore, int losingTeamScore)
        {
            if (!match.IsByeMatch)
            {
                // TODO Handle Round Robin Win Logic
                _matchManager.ConvertMatchToPostMatch(tournament, match, winningTeam.Name, winningTeamScore, losingTeam.Name, losingTeamScore, match.IsByeMatch);
            }
            else
            {
                _matchManager.ConvertMatchToPostMatch(tournament, match, winningTeam.Name, 0, "BYE", 0, match.IsByeMatch);
            }

            _tournamentManager.SaveAndReloadTournamentsDatabase();

            if (match.IsByeMatch)
            {
                return $"{winningTeam.Name} has had their Bye week match recorded for data purposes. This is required to lock the rounds with when a tournament has an odd number of teams.";
            }

            return $"Round Robin win reported for {winningTeam.Name} with score {winningTeamScore} against {losingTeam.Name} with score {losingTeamScore}.";
        }
    }
}
