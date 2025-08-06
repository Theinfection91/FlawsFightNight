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
            // Check if team exists across all tournaments
            if (!_teamManager.IsTeamNameUnique(winningTeamName))
            {
                return $"The team '{winningTeamName}' does not exist or is not registered in any tournament.";
            }

            // Grab the tournament associated with the match
            Tournament tournament = _tournamentManager.GetTournamentFromTeamName(winningTeamName);

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
                    HandleRoundRobinWin(tournament, match, winningTeam, losingTeam, winningTeamScore, losingTeamScore);
                    break;
                case TournamentType.SingleElimination:
                case TournamentType.DoubleElimination:
                    // Handle Single/Double Elimination specific logic

                    break;
                default:
                    return "Tournament type not supported for win reporting.";
            }

            return "TODO";
        }

        private string HandleRoundRobinWin(Tournament tournament, Match match, Team winningTeam, Team losingTeam, int winningTeamScore, int losingTeamScore)
        {
            // TODO Handle Round Robin Win Logic
            _matchManager.ConvertMatchToPostMatch(tournament, match, winningTeam.Name, winningTeamScore, losingTeam.Name, losingTeamScore, match.IsByeMatch);

            return $"Round Robin win reported for {winningTeam.Name} with score {winningTeamScore} against {losingTeam.Name} with score {losingTeamScore}.";
        }

        // Probably gonna need a resolver since there are different types of tournaments and different ways to handle what happens when a team wins. In Ladder, teams move up and down. In Round Robin, teams just lock their scores til the round is done. In SE/DE, teams are eliminated or advance to the next round.
    }
}
