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
using Discord.WebSocket;

namespace FlawsFightNight.CommandsLogic.MatchCommands
{
    public class ReportRoundRobinWinLogic : Logic
    {
        private EmbedManager _embedManager;
        private GitBackupManager _gitBackupManager;
        private MatchManager _matchManager;
        private TeamManager _teamManager;
        private TournamentManager _tournamentManager;
        public ReportRoundRobinWinLogic(EmbedManager embedManager, GitBackupManager gitBackupManager, MatchManager matchManager, TeamManager teamManager, TournamentManager tournamentManager) : base("Report Win")
        {
            _embedManager = embedManager;
            _gitBackupManager = gitBackupManager;
            _matchManager = matchManager;
            _teamManager = teamManager;
            _tournamentManager = tournamentManager;
        }

        public Embed ReportRoundRobinWinProcess(SocketInteractionContext context, string matchId, string winningTeamName, int winningTeamScore, int losingTeamScore)
        {
            if (losingTeamScore > winningTeamScore)
            {
                return _embedManager.ErrorEmbed(Name, "The losing team score cannot be greater than the winning team score.");
            }

            // Cannot try to report Bye as a team
            if (winningTeamName.Equals("Bye", StringComparison.OrdinalIgnoreCase))
            {
                return _embedManager.ErrorEmbed(Name, $"You may not try to report a Bye team as the winner of a Bye match. \n\nUser input: {winningTeamName}");
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

            // Check if match exists in database
            if (!_matchManager.IsMatchIdInDatabase(matchId))
            {
                return _embedManager.ErrorEmbed(Name, $"The match with ID '{matchId}' does not exist.");
            }

            // Grab the match associated with report
            Match match = _matchManager.GetMatchByMatchIdResolver(tournament, matchId);

            // Check if team is part of the match
            if (!_matchManager.IsTeamInMatch(match, winningTeamName))
            {
                return _embedManager.ErrorEmbed(Name, $"The team '{winningTeamName}' is not part of the match with ID '{matchId}'.");
            }

            switch (tournament.Type)
            {
                case TournamentType.RoundRobin:
                    switch (tournament.RoundRobinMatchType)
                    {
                        case RoundRobinMatchType.Normal:
                            return RoundRobinNormalReportWinProcess(context, winningTeamName, winningTeamScore, losingTeamScore, tournament, match);
                        case RoundRobinMatchType.Open:
                            return RoundRobinOpenReportWinProcess(context, winningTeamName, winningTeamScore, losingTeamScore, tournament, match);
                        default:
                            return _embedManager.ErrorEmbed(Name, "The Round Robin match type is not recognized.");
                    }
                case TournamentType.Ladder:
                    return _embedManager.ToDoEmbed("Ladder Report Win logic is not yet implemented.");
                case TournamentType.SingleElimination:
                case TournamentType.DoubleElimination:
                    return _embedManager.ToDoEmbed("Single/Double Elimination Report Win logic is not yet implemented.");
                default:
                    return _embedManager.ErrorEmbed(Name, "The tournament type is not recognized.");
            }
        }

        private Embed RoundRobinNormalReportWinProcess(SocketInteractionContext context, string winningTeamName, int winningTeamScore, int losingTeamScore, Tournament tournament, Match match)
        {
            // Check if the tournament is Normal Round Robin and the round is marked complete
            if (tournament.IsRoundComplete)
            {
                return _embedManager.ErrorEmbed(Name, "The tournament is showing the round has been marked as complete.");
            }

            // Check if the team has a match scheduled
            if (!_matchManager.IsMatchMadeForTeamResolver(tournament, winningTeamName))
            {
                return _embedManager.ErrorEmbed(Name, $"The team '{winningTeamName}' does not have a match scheduled.");
            }

            // Grab the winning team
            Team? winningTeam = _teamManager.GetTeamByName(tournament, winningTeamName);

            // Grab the losing team
            Team? losingTeam = _teamManager.GetTeamByName(_matchManager.GetLosingTeamName(match, winningTeamName));

            // Check if invoker is on winning team (or guild admin)
            if (context.User is not SocketGuildUser guildUser)
            {
                return _embedManager.ErrorEmbed(Name, "Only members of the guild may use this command.");
            }
            bool isGuildAdmin = guildUser.GuildPermissions.Administrator;
            if (!_teamManager.IsDiscordIdOnTeam(winningTeam, context.User.Id) && !isGuildAdmin)
            {
                return _embedManager.ErrorEmbed(Name, $"You are not a member of the team '{winningTeamName}', or an admin on this server, and cannot report a win for them.");
            }

            if (!match.IsByeMatch)
            {
                // Convert match to post-match and record win/loss
                _matchManager.ConvertMatchToPostMatchResolver(tournament, match, winningTeam.Name, winningTeamScore, losingTeam.Name, losingTeamScore, match.IsByeMatch);
                _teamManager.RecordTeamWin(winningTeam, winningTeamScore);
                _teamManager.RecordTeamLoss(losingTeam, losingTeamScore);
            }
            else
            {
                _matchManager.ConvertMatchToPostMatchResolver(tournament, match, winningTeam.Name, 0, "BYE", 0, match.IsByeMatch);
            }

            // Adjust ranks of remaining teams
            tournament.SetRanksByTieBreakerLogic();

            // Save and reload the tournament database
            _tournamentManager.SaveAndReloadTournamentsDatabase();

            // Backup to git repo
            _gitBackupManager.CopyAndBackupFilesToGit();

            if (match.IsByeMatch)
            {
                return _embedManager.ReportByeMatch(tournament, match, isGuildAdmin);
            }

            return _embedManager.ReportWinSuccess(tournament, match, winningTeam, winningTeamScore, losingTeam, losingTeamScore, isGuildAdmin);
        }

        private Embed RoundRobinOpenReportWinProcess(SocketInteractionContext context, string winningTeamName, int winningTeamScore, int losingTeamScore, Tournament tournament, Match match)
        {
            // Check if the team has a match scheduled
            if (!_matchManager.IsMatchMadeForTeamResolver(tournament, winningTeamName))
            {
                return _embedManager.ErrorEmbed(Name, $"The team '{winningTeamName}' does not have a match scheduled.");
            }

            // Grab the winning team
            Team? winningTeam = _teamManager.GetTeamByName(tournament, winningTeamName);

            // Grab the losing team
            Team? losingTeam = _teamManager.GetTeamByName(_matchManager.GetLosingTeamName(match, winningTeamName));

            // Check if invoker is on winning team (or guild admin)
            if (context.User is not SocketGuildUser guildUser)
            {
                return _embedManager.ErrorEmbed(Name, "Only members of the guild may use this command.");
            }
            bool isGuildAdmin = guildUser.GuildPermissions.Administrator;
            if (!_teamManager.IsDiscordIdOnTeam(winningTeam, context.User.Id) && !isGuildAdmin)
            {
                return _embedManager.ErrorEmbed(Name, $"You are not a member of the team '{winningTeamName}', or an admin on this server, and cannot report a win for them.");
            }
            if (!match.IsByeMatch)
            {
                // Convert match to post-match and record win/loss
                _matchManager.ConvertMatchToPostMatchResolver(tournament, match, winningTeam.Name, winningTeamScore, losingTeam.Name, losingTeamScore, match.IsByeMatch);

                _teamManager.RecordTeamWin(winningTeam, winningTeamScore);
                _teamManager.RecordTeamLoss(losingTeam, losingTeamScore);
            }
            else
            {
                // Convert to Open Post-Match
                _matchManager.ConvertMatchToPostMatchResolver(tournament, match, winningTeam.Name, 0, "BYE", 0, match.IsByeMatch);
            }

            // Adjust ranks of remaining teams
            tournament.SetRanksByTieBreakerLogic();

            // Save and reload the tournament database
            _tournamentManager.SaveAndReloadTournamentsDatabase();

            // Backup to git repo
            _gitBackupManager.CopyAndBackupFilesToGit();

            return _embedManager.ReportWinSuccess(tournament, match, winningTeam, winningTeamScore, losingTeam, losingTeamScore, isGuildAdmin);
        }
    }
}
