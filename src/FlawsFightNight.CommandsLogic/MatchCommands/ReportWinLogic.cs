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

            if (match == null)
            {
                return _embedManager.ErrorEmbed(Name, $"The match with ID '{matchId}' could not be found in the tournament '{tournament.Name}'. Make sure you are not trying to report a match that has already been played.");
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

            // Record wins and losses
            _teamManager.RecordTeamWin(winningTeam, winningTeamScore);
            _teamManager.RecordTeamLoss(losingTeam, losingTeamScore);

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

            switch (tournament.Type)
            {
                case TournamentType.Ladder:
                    return LadderReportWinProcess(winningTeam, winningTeamScore, losingTeam, losingTeamScore, tournament, match, isGuildAdmin);
                case TournamentType.RoundRobin:
                    switch (tournament.RoundRobinMatchType)
                    {
                        case RoundRobinMatchType.Normal:
                            return RoundRobinNormalReportWinProcess(winningTeam, winningTeamScore, losingTeam, losingTeamScore, tournament, match, isGuildAdmin);
                        case RoundRobinMatchType.Open:
                            return RoundRobinOpenReportWinProcess(winningTeam, winningTeamScore, losingTeam, losingTeamScore, tournament, match, isGuildAdmin);
                        default:
                            return _embedManager.ErrorEmbed(Name, "The Round Robin match type is not recognized.");
                    }
                case TournamentType.SingleElimination:
                case TournamentType.DoubleElimination:
                    return _embedManager.ToDoEmbed("Single/Double Elimination Report Win logic is not yet implemented.");
                default:
                    return _embedManager.ErrorEmbed(Name, "The tournament type is not recognized.");
            }
        }

        private Embed RoundRobinNormalReportWinProcess(Team winningTeam, int winningTeamScore, Team losingTeam, int losingTeamScore, Tournament tournament, Match match, bool isGuildAdmin)
        {
            // Check if the tournament is Normal Round Robin and the round is marked complete
            if (tournament.IsRoundComplete)
            {
                return _embedManager.ErrorEmbed(Name, "The tournament is showing the round has been marked as complete.");
            }

            if (!match.IsByeMatch)
            {
                // Convert match to post-match and record win/loss
                _matchManager.ConvertMatchToPostMatchResolver(tournament, match, winningTeam.Name, winningTeamScore, losingTeam.Name, losingTeamScore, match.IsByeMatch);
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

        private Embed RoundRobinOpenReportWinProcess(Team winningTeam, int winningTeamScore, Team losingTeam, int losingTeamScore, Tournament tournament, Match match, bool isGuildAdmin)
        {
            // Check if the team has a match scheduled
            //if (!_matchManager.IsMatchMadeForTeamResolver(tournament, winningTeamName))
            //{
            //    return _embedManager.ErrorEmbed(Name, $"The team '{winningTeamName}' does not have a match scheduled.");
            //}

            if (!match.IsByeMatch)
            {
                // Convert match to post-match and record win/loss
                _matchManager.ConvertMatchToPostMatchResolver(tournament, match, winningTeam.Name, winningTeamScore, losingTeam.Name, losingTeamScore, match.IsByeMatch);
            }
            else
            {
                // Convert to Open Post-Match
                // Shouldn't be possible to have a Bye match in Open RR, but just in case
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

        private Embed LadderReportWinProcess(Team winningTeam, int winningTeamScore, Team losingTeam, int losingTeamScore, Tournament tournament, Match match, bool isGuildAdmin)
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

            // TODO: Add Challenge Rank comparison correction like Ladderbot4. Overtime if other teams play their challenges and report faster than older challenges are reported then there will be inconsistencies with how the LiveView will show the challenge ranks and what the team ranks actually are.

            // Convert match to post-match
            _matchManager.ConvertMatchToPostMatchResolver(tournament, match, winningTeam.Name, winningTeamScore, losingTeam.Name, losingTeamScore);

            // Save and reload the tournament database
            _tournamentManager.SaveAndReloadTournamentsDatabase();

            // Backup to git repo
            _gitBackupManager.CopyAndBackupFilesToGit();

            return _embedManager.ReportWinSuccess(tournament, match, winningTeam, winningTeamScore, losingTeam, losingTeamScore, isGuildAdmin);
        }
    }
}
