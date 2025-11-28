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
using FlawsFightNight.Core.Models.Tournaments;
using FlawsFightNight.Core.Interfaces;
using FlawsFightNight.Core.Models.MatchLogs;

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
            _tournamentManager.LoadTournamentsDatabase();
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

            // Check if the tournament is IRoundBased and if the round is marked complete
            if (tournament is IRoundBased roundBasedTournament)
            {
                if (roundBasedTournament.IsRoundComplete)
                {
                    return _embedManager.ErrorEmbed(Name, "The tournament is showing the round has been marked as complete.");
                }
            }

            // Check if match exists in database
            if (!_matchManager.IsMatchIdInDatabase(matchId))
            {
                return _embedManager.ErrorEmbed(Name, $"The match with ID '{matchId}' does not exist.");
            }

            // Grab the match associated with report
            Match? match = tournament.MatchLog.GetMatchById(matchId);
            if (match == null)
            {
                return _embedManager.ErrorEmbed(Name, $"The match with ID '{matchId}' could not be found in the tournament '{tournament.Name}'. Make sure you are not trying to report a match that has already been played.");
            }

            // Check if match is bye match, system will handle those.
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

            // Normal RR check to make sure match is in current round being played
            if (tournament is IRoundBased rbTournament && tournament.MatchLog is NormalRoundRobinMatchLog rrLog && !rrLog.DoesRoundContainMatchId(rbTournament.CurrentRound, match.Id))
            {
                return _embedManager.ErrorEmbed(Name, $"The match with ID '{matchId}' is not part of the current round '{rbTournament.CurrentRound}' being played in the tournament '{tournament.Name}'. You may only report matches that are part of the current round.");
            }

            // Record wins and losses
            winningTeam.RecordWin(winningTeamScore);
            losingTeam.RecordLoss(losingTeamScore);

            // Convert match to post-match
            tournament.MatchLog.ConvertMatchToPostMatch(tournament, match, winningTeam.Name, winningTeamScore, losingTeam.Name, losingTeamScore);

            if (tournament is NormalLadderTournament ladderTournament)
            {
                if (_matchManager.IsWinningTeamChallenger(match, winningTeam))
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
                    //ladderTournament.ReassignRanks();
                }
                winningTeam.IsChallengeable = true;
                losingTeam.IsChallengeable = true;
            }

            // Adjust ranks
            tournament.AdjustRanks();

            // Save and reload the tournament database
            _tournamentManager.SaveAndReloadTournamentsDatabase();

            // Backup to git repo
            _gitBackupManager.CopyAndBackupFilesToGit();

            return _embedManager.ReportWinSuccess(tournament, match, winningTeam, winningTeamScore, losingTeam, losingTeamScore, isGuildAdmin);
        }
    }
}
