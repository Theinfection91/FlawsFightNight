using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using FlawsFightNight.Commands;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Interfaces;
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
    public class CancelChallengeHandler : CommandHandler
    {
        private EmbedFactory _embedFactory;
        private GitBackupService _gitBackupService;
        private MatchService _matchService;
        private TeamService _teamService;
        private TournamentService _tournamentService;
        public CancelChallengeHandler(EmbedFactory embedFactory, GitBackupService gitBackupService, MatchService matchService, TeamService teamService, TournamentService tournamentService) : base("Cancel Challenge")
        {
            _embedFactory = embedFactory;
            _gitBackupService = gitBackupService;
            _matchService = matchService;
            _teamService = teamService;
            _tournamentService = tournamentService;
        }

        public async Task<Embed> CancelChallengeProcess(SocketInteractionContext context, string challengerTeamName)
        {
            if (!_teamService.DoesTeamExist(challengerTeamName))
            {
                return _embedFactory.ErrorEmbed(Name, $"No team found with the name '{challengerTeamName}'. Please check the name and try again.");
            }

            // Grab teams
            Team? challengerTeam = _teamService.GetTeamByName(challengerTeamName);

            // Check if invoker is on challenger team (or guild admin)
            if (context.User is not SocketGuildUser guildUser)
            {
                return _embedFactory.ErrorEmbed(Name, "Only members of the guild may use this command.");
            }
            // Store admin status for embed message use
            bool isGuildAdmin = guildUser.GuildPermissions.Administrator;
            // Check if user is on the challenger team
            if (!_teamService.IsDiscordIdOnTeam(challengerTeam, context.User.Id) && !isGuildAdmin)
            {
                return _embedFactory.ErrorEmbed(Name, $"You are not a member of the team '{challengerTeam.Name}', or an admin on this server, and cannot cancel a challenge on their behalf.");
            }

            // Grab tournament from challenger team
            var tournament = _tournamentService.GetTournamentFromTeamName(challengerTeamName);
            if (tournament == null)
            {
                // Shouldn't be possible, but just in case
                return _embedFactory.ErrorEmbed(Name, $"The team '{challengerTeam.Name}' is not registered in any tournament. Please register the team to a ladder tournament before canceling challenges.");
            }

            // Ensure tournament is a ladder type 
            if (!tournament.Type.Equals(TournamentType.NormalLadder) && !tournament.Type.Equals(TournamentType.DSRLadder))
            {
                return _embedFactory.ErrorEmbed(Name, $"Challenges can only be sent in Normal or DSR Ladder tournaments. The tournament '{tournament.Name}' is of type '{tournament.Type}'.");
            }

            // Ensure tournament is running
            if (!tournament.IsRunning)
            {
                return _embedFactory.ErrorEmbed(Name, $"The tournament '{tournament.Name}' is not currently running. Challenges can only be canceled in running tournaments.");
            }

            // Check if there is a pending challenge between these two teams
            if (!(tournament.MatchLog as IChallengeLog).IsTeamChallenger(challengerTeam))
            {
                return _embedFactory.ErrorEmbed(Name, $"The team '{challengerTeam.Name}' does not have any pending challenges sent out to cancel.");
            }

            // Get the pending match
            var pendingMatch = (tournament.MatchLog as IChallengeLog).GetChallengeMatch(challengerTeam);

            // Grab challenged team
            var challengedTeam = tournament.GetTeam(pendingMatch.Challenge.Challenged);

            // Reset challenge status on both teams
            challengerTeam.IsChallengeable = true;
            challengedTeam.IsChallengeable = true;

            // Send direct message notifications to each member of both teams about the canceled challenge
            _matchService.SendChallengeCancelNotificationProcess(tournament, pendingMatch, challengerTeam, challengedTeam);

            // Cancel the challenge, remove match
            tournament.MatchLog.RemoveMatch(pendingMatch);

            // Save and reload the tournaments database
            await _tournamentService.SaveAndReloadTournamentDataFiles(tournament);
            
            // Backup to git repo
            _gitBackupService.EnqueueBackup();
            
            return _embedFactory.CancelChallengeSuccess(tournament, pendingMatch, isGuildAdmin);
        }
    }
}
