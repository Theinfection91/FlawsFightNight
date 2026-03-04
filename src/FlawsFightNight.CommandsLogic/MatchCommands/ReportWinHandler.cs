using Discord;
using Discord.Interactions;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using FlawsFightNight.Core.Models.Tournaments;
using FlawsFightNight.Core.Interfaces;
using FlawsFightNight.Core.Models.MatchLogs;
using FlawsFightNight.Commands;

namespace FlawsFightNight.Commands.MatchCommands
{
    public class ReportWinHandler : CommandHandler
    {
        private readonly EmbedFactory _embedFactory;
        private readonly GitBackupService _gitBackupService;
        private readonly MatchService _matchService;
        private readonly MemberService _memberService;
        private readonly TeamService _teamService;
        private readonly TournamentService _tournamentService;
        public ReportWinHandler(EmbedFactory embedService, GitBackupService gitBackupService, MatchService matchService, MemberService memberService, TeamService teamService, TournamentService tournamentService) : base("Report Win")
        {
            _embedFactory = embedService;
            _gitBackupService = gitBackupService;
            _matchService = matchService;
            _memberService = memberService;
            _teamService = teamService;
            _tournamentService = tournamentService;
        }

        public async Task<Embed> ReportWinProcess(SocketInteractionContext context, string matchId, string winningTeamName, int winningTeamScore, int losingTeamScore)
        {
            if (losingTeamScore > winningTeamScore)
            {
                return _embedFactory.ErrorEmbed(Name, "The losing team score cannot be greater than the winning team score.");
            }

            // Cannot try to report Bye as a team
            if (winningTeamName.Equals("Bye", StringComparison.OrdinalIgnoreCase))
            {
                return _embedFactory.ErrorEmbed(Name, $"You may not try to report the team as any variation of 'Bye' since that name is forbidden as a team name. \n\nUser input: {winningTeamName}");
            }

            // Check if team exists across all tournaments
            if (!_teamService.DoesTeamExist(winningTeamName))
            {
                return _embedFactory.ErrorEmbed(Name, $"The team '{winningTeamName}' does not exist across any tournament in the database.");
            }

            // Grab the tournament associated with the match
            Tournament tournament = _tournamentService.GetTournamentFromTeamName(winningTeamName);

            if (!tournament.IsRunning)
            {
                return _embedFactory.ErrorEmbed(Name, $"The tournament '{tournament.Name}' is not currently running.");
            }

            // Check if the tournament is IRoundBased and if the round is marked complete
            if (tournament is IRoundBased roundBasedTournament)
            {
                if (roundBasedTournament.IsRoundComplete)
                {
                    return _embedFactory.ErrorEmbed(Name, "The tournament is showing the round has been marked as complete.");
                }
            }

            // Check if match exists in database
            if (!_matchService.IsMatchIdInDatabase(matchId))
            {
                return _embedFactory.ErrorEmbed(Name, $"The match with ID '{matchId}' does not exist.");
            }

            // Grab the match associated with report
            Match? match = tournament.MatchLog.GetMatchById(matchId);
            if (match == null)
            {
                return _embedFactory.ErrorEmbed(Name, $"The match with ID '{matchId}' could not be found in the tournament '{tournament.Name}'. Make sure you are not trying to report a match that has already been played.");
            }

            // Check if match is bye match, system will handle those.
            if (match.IsByeMatch)
            {
                return _embedFactory.ErrorEmbed(Name, $"The match with ID '{matchId}' is a Bye match and cannot be reported manually. Bye matches are automatically handled by the system when a round ends in a Normal Round Robin tournament.");
            }

            // Check if team is part of the match
            if (!_matchService.IsTeamInMatch(match, winningTeamName))
            {
                return _embedFactory.ErrorEmbed(Name, $"The team '{winningTeamName}' is not part of the match with ID '{matchId}'.");
            }

            // Grab the winning team
            Team? winningTeam = _teamService.GetTeamByName(tournament, winningTeamName);

            // Grab the losing team
            Team? losingTeam = _teamService.GetTeamByName(_matchService.GetLosingTeamName(match, winningTeamName));

            // Check if invoker is on winning team (or guild admin)
            if (context.User is not SocketGuildUser guildUser)
            {
                return _embedFactory.ErrorEmbed(Name, "Only members of the guild may use this command.");
            }
            bool isGuildAdmin = guildUser.GuildPermissions.Administrator;
            if (!_teamService.IsDiscordIdOnTeam(winningTeam, context.User.Id) && !isGuildAdmin)
            {
                return _embedFactory.ErrorEmbed(Name, $"You are not a member of the team '{winningTeam.Name}', or an admin on this server, and cannot report a win for them.");
            }

            // Normal RR check to make sure match is in current round being played
            if (tournament is IRoundBased rbTournament && tournament.MatchLog is NormalRoundRobinMatchLog rrLog && !rrLog.DoesRoundContainMatchId(rbTournament.CurrentRound, match.Id))
            {
                return _embedFactory.ErrorEmbed(Name, $"The match with ID '{matchId}' is not part of the current round '{rbTournament.CurrentRound}' being played in the tournament '{tournament.Name}'. You may only report matches that are part of the current round.");
            }

            // Record wins and losses
            winningTeam.RecordWin(winningTeamScore);
            losingTeam.RecordLoss(losingTeamScore);

            // Convert match to post-match
            tournament.MatchLog.ConvertMatchToPostMatch(tournament, match, winningTeam.Name, winningTeamScore, losingTeam.Name, losingTeamScore);

            // Handle normal ladder tournament challenge procedures
            if (tournament is NormalLadderTournament ladderTournament)
            {
                if (_matchService.IsWinningTeamChallenger(match, winningTeam))
                {
                    winningTeam.Rank = losingTeam.Rank;
                    losingTeam.Rank++;
                    foreach (var team in ladderTournament.Teams)
                    {
                        if (team.Rank.Equals(losingTeam.Rank) && !team.Equals(losingTeam))
                        {
                            team.Rank++;
                        }
                    }
                }
            }

            // Handle DSR Ladder tournament procedures
            int winningTeamRatingChange = 0;
            int losingTeamRatingChange = 0;
            if (tournament is DSRLadderTournament dsrLadderTournament)
            {
                // Run the calculator and output rating changes
                dsrLadderTournament.HandleTeamRatingChange(winningTeam, losingTeam, winningTeamScore, losingTeamScore, out winningTeamRatingChange, out losingTeamRatingChange);

                // Grab the post match and record the rating change of teams
                (tournament.MatchLog as DSRLadderMatchLog)?.RecordRatingChangeToPostMatch(matchId, winningTeamRatingChange, losingTeamRatingChange);
            }

            winningTeam.IsChallengeable = true;
            losingTeam.IsChallengeable = true;

            // Update member stats for all members in the tournament
            _memberService.RecordWinLossForMembers(winningTeam, losingTeam);

            // Adjust ranks
            tournament.AdjustRanks();

            // Save and reload databases
            await _tournamentService.SaveAndReloadTournamentDataFiles(tournament);
            await _memberService.SaveAndReloadMemberProfiles();

            // Backup to git repo
            _gitBackupService.EnqueueBackup();

            return _embedFactory.ReportWinSuccess(tournament, match, winningTeam, winningTeamScore, losingTeam, losingTeamScore, isGuildAdmin, winningTeamRatingChange, losingTeamRatingChange);
        }
    }
}
