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
    public class ReportWinLogic : Logic
    {
        private EmbedManager _embedManager;
        private GitBackupManager _gitBackupManager;
        private MatchManager _matchManager;
        private TeamManager _teamManager;
        private TournamentManager _tournamentManager;
        public ReportWinLogic(EmbedManager embedManager, GitBackupManager gitBackupManager, MatchManager matchManager, TeamManager teamManager, TournamentManager tournamentManager) : base("Report Win")
        {
            _embedManager = embedManager;
            _gitBackupManager = gitBackupManager;
            _matchManager = matchManager;
            _teamManager = teamManager;
            _tournamentManager = tournamentManager;
        }

        public Embed ReportWinProcess(SocketInteractionContext context, string matchId, string winningTeamName, int winningTeamScore, int losingTeamScore)
        {
            if (losingTeamScore > winningTeamScore)
            {
                return _embedManager.ErrorEmbed(Name, "The losing team score cannot be greater than the winning team score.");
            }

            // Cannot try to report Bye as a team
            if (winningTeamName.Equals("Bye", StringComparison.OrdinalIgnoreCase))
            {
                return _embedManager.ErrorEmbed(Name, $"You may not try to report the team as any variation of 'Bye' since that name is forbidden as a team name. \n\nUser input: {winningTeamName}");
            }

            // Check if team exists across all tournaments
            if (!_teamManager.DoesTeamExist(winningTeamName))
            {
                return _embedManager.ErrorEmbed(Name, $"The team '{winningTeamName}' does not exist across any tournament in the database.");
            }

            // Grab the tournament associated with the match
            Tournament tournament = _tournamentManager.GetTournamentFromTeamName(winningTeamName);

            if (!tournament.IsRunning)
            {
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' is not currently running.");
            }

            // Check if the tournament is Normal Round Robin and the round is marked complete
            if (tournament.IsRoundComplete && tournament.Type.Equals(TournamentType.RoundRobin) && tournament.RoundRobinMatchType.Equals(RoundRobinMatchType.Normal))
            {
                return _embedManager.ErrorEmbed(Name, "The tournament is showing the round has been marked as complete.");
            }

            // Check if match exists in database
            if (!_matchManager.IsMatchIdInDatabase(matchId))
            {
                return _embedManager.ErrorEmbed(Name, $"The match with ID '{matchId}' does not exist.");
            }

            // Grab the match associated with report
            Match match = _matchManager.GetMatchByMatchIdResolver(tournament, matchId);

            if (match == null)
            {
                return _embedManager.ErrorEmbed(Name, $"The match with ID '{matchId}' could not be found in the tournament '{tournament.Name}'. Make sure you are not trying to report a match that has already been played.");
            }

            // Check if match is bye match, in new version of bot we are not allowing reporting of bye matches as system will handle it.
            if (match.IsByeMatch)
            {
                return _embedManager.ErrorEmbed(Name, $"The match with ID '{matchId}' is a Bye match and cannot be reported manually. Bye matches are automatically handled by the system when a round ends in a Normal Round Robin tournament.");
            }

            // Check if team is part of the match
            if (!_matchManager.IsTeamInMatch(match, winningTeamName))
            {
                return _embedManager.ErrorEmbed(Name, $"The team '{winningTeamName}' is not part of the match with ID '{matchId}'.");
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
                return _embedManager.ErrorEmbed(Name, $"You are not a member of the team '{winningTeam.Name}', or an admin on this server, and cannot report a win for them.");
            }

            // Needs Normal RR checks like making sure match is in current round being played
            if (tournament.Type.Equals(TournamentType.RoundRobin) && tournament.RoundRobinMatchType.Equals(RoundRobinMatchType.Normal) && !_matchManager.IsMatchInCurrentRound(tournament, match.Id))
            {
                return _embedManager.ErrorEmbed(Name, $"The match with ID '{matchId}' is not part of the current round '{tournament.CurrentRound}' being played in the tournament '{tournament.Name}'. You may only report matches that are part of the current round.");
            }

            // Process report win based on tournament type
            switch (tournament.Type)
            {
                case TournamentType.Ladder:
                    LadderReportWinProcess(winningTeam, winningTeamScore, losingTeam, losingTeamScore, tournament, match, isGuildAdmin);
                    break;
                case TournamentType.RoundRobin:
                    RoundRobinReportWinProcess(winningTeam, winningTeamScore, losingTeam, losingTeamScore, tournament, match, isGuildAdmin);
                    break;
                case TournamentType.SingleElimination:
                case TournamentType.DoubleElimination:
                    return _embedManager.ToDoEmbed("Single/Double Elimination Report Win logic is not yet implemented.");
            }

            // Record wins and losses
            _teamManager.RecordTeamWin(winningTeam, winningTeamScore);
            _teamManager.RecordTeamLoss(losingTeam, losingTeamScore);

            // Convert match to post-match
            _matchManager.ConvertMatchToPostMatchResolver(tournament, match, winningTeam.Name, winningTeamScore, losingTeam.Name, losingTeamScore);

            // Save and reload the tournament database
            _tournamentManager.SaveAndReloadTournamentsDatabase();

            // Backup to git repo
            _gitBackupManager.CopyAndBackupFilesToGit();

            return _embedManager.ReportWinSuccess(tournament, match, winningTeam, winningTeamScore, losingTeam, losingTeamScore, isGuildAdmin);
        }

        private void RoundRobinReportWinProcess(Team winningTeam, int winningTeamScore, Team losingTeam, int losingTeamScore, Tournament tournament, Match match, bool isGuildAdmin)
        {
            // Convert match to post-match and record win/loss
            _matchManager.ConvertMatchToPostMatchResolver(tournament, match, winningTeam.Name, winningTeamScore, losingTeam.Name, losingTeamScore, match.IsByeMatch);

            // Adjust ranks of remaining teams
            tournament.SetRanksByTieBreakerLogic();
        }

        private void LadderReportWinProcess(Team winningTeam, int winningTeamScore, Team losingTeam, int losingTeamScore, Tournament tournament, Match match, bool isGuildAdmin)
        {
            // If winning team is challenger then rank change will occur
            if (_matchManager.IsWinningTeamChallenger(match, winningTeam))
            {
                // Winning team takes the rank of the losing team
                winningTeam.Rank = losingTeam.Rank;
                // Losing team moves down one rank
                losingTeam.Rank++;

                // Reassign ranks of entire tournament
                _matchManager.ReassignRanksInLadderTournament(tournament);
            }
            else
            {
                // No rank change, no action needed
            }

            // Change each Team's IsChallengeable status back to true
            winningTeam.IsChallengeable = true;
            losingTeam.IsChallengeable = true;

            // Run challenge rank comparison for tournament to make sure LiveView displays correct rank for team in challenges
            _matchManager.ChallengeRankComparisonProcess(tournament);
        }
    }
}
