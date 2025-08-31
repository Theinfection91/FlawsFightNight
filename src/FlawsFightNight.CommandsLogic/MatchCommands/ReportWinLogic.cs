using Discord;
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
        private EmbedManager _embedManager;
        private MatchManager _matchManager;
        private TeamManager _teamManager;
        private TournamentManager _tournamentManager;
        public ReportWinLogic(EmbedManager embedManager, MatchManager matchManager, TeamManager teamManager, TournamentManager tournamentManager) : base("Report Win")
        {
            _embedManager = embedManager;
            _matchManager = matchManager;
            _teamManager = teamManager;
            _tournamentManager = tournamentManager;
        }

        public Embed ReportWinProcess(SocketInteractionContext context, string winningTeamName, int winningTeamScore, int losingTeamScore)
        {
            if (losingTeamScore > winningTeamScore)
            {
                return _embedManager.ErrorEmbed(Name, "The losing team score cannot be greater than the winning team score.");
            }

            // Cannot try to report Bye as a team
            if (winningTeamName.Equals("Bye", StringComparison.OrdinalIgnoreCase))
            {
                return _embedManager.ErrorEmbed(Name , $"You may not try to report a Bye team as the winner of a Bye match. \n\nUser input: {winningTeamName}");
            }

            // Check if team exists across all tournaments
            if (!_teamManager.DoesTeamExist(winningTeamName))
            {
                return _embedManager.ErrorEmbed(Name, $"The team '{winningTeamName}' does not exist or is not registered in any tournament.");
            }

            // Grab the tournament associated with the match
            Tournament tournament = _tournamentManager.GetTournamentFromTeamName(winningTeamName);

            if (!tournament.IsRunning)
            {
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' is not currently running.");
            }

            if (tournament.IsRoundComplete) return _embedManager.ErrorEmbed(Name, "The tournament is showing the round has been marked as complete.");

            // Check if the team has a match scheduled
            if (!_matchManager.IsMatchMadeForTeam(tournament, winningTeamName))
            {
                return _embedManager.ErrorEmbed(Name, $"The team '{winningTeamName}' does not have a match scheduled.");
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
                return _embedManager.ErrorEmbed(Name, $"You are not a member of the team '{winningTeamName}' and cannot report a win for them.");
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
            return _embedManager.ErrorEmbed(Name, "Should not have reached this message.");
        }

        private Embed HandleRoundRobinWin(Tournament tournament, Match match, Team winningTeam, Team losingTeam, int winningTeamScore, int losingTeamScore)
        {
            if (!match.IsByeMatch)
            {
                // TODO Handle Round Robin Win Logic
                _matchManager.ConvertMatchToPostMatch(tournament, match, winningTeam.Name, winningTeamScore, losingTeam.Name, losingTeamScore, match.IsByeMatch);
                _teamManager.RecordTeamWin(winningTeam, winningTeamScore);
                _teamManager.RecordTeamLoss(losingTeam, losingTeamScore);
            }
            else
            {
                _matchManager.ConvertMatchToPostMatch(tournament, match, winningTeam.Name, 0, "BYE", 0, match.IsByeMatch);
            }

            _tournamentManager.SaveAndReloadTournamentsDatabase();

            if (match.IsByeMatch)
            {
                return _embedManager.ReportByeMatch(tournament, match);
            }

            return _embedManager.ReportWinSuccess(tournament, match, winningTeam, winningTeamScore, losingTeam, losingTeamScore);
        }
    }
}
