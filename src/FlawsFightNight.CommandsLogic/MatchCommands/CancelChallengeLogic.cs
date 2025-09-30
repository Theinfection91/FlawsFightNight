using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.MatchCommands
{
    public class CancelChallengeLogic : Logic
    {
        private EmbedManager _embedManager;
        private GitBackupManager _gitBackupManager;
        private MatchManager _matchManager;
        private TeamManager _teamManager;
        private TournamentManager _tournamentManager;
        public CancelChallengeLogic(EmbedManager embedManager, GitBackupManager gitBackupManager, MatchManager matchManager, TeamManager teamManager, TournamentManager tournamentManager) : base("Cancel Challenge")
        {
            _embedManager = embedManager;
            _gitBackupManager = gitBackupManager;
            _matchManager = matchManager;
            _teamManager = teamManager;
            _tournamentManager = tournamentManager;
        }

        public Embed CancelChallengeProcess(SocketInteractionContext context, string challengerTeamName)
        {
            if (!_teamManager.DoesTeamExist(challengerTeamName))
            {
                return _embedManager.ErrorEmbed(Name, $"No team found with the name '{challengerTeamName}'. Please check the name and try again.");
            }

            // Grab teams
            Team? challengerTeam = _teamManager.GetTeamByName(challengerTeamName);

            // Grab tournament from challenger team
            Tournament? tournament = _tournamentManager.GetTournamentFromTeamName(challengerTeamName);
            if (tournament == null)
            {
                // Shouldn't be possible, but just in case
                return _embedManager.ErrorEmbed(Name, $"The team '{challengerTeam.Name}' is not registered in any tournament. Please register the team to a ladder tournament before canceling challenges.");
            }

            // Ensure tournament is a ladder type
            if (!tournament.Type.Equals(TournamentType.Ladder))
            {
                return _embedManager.ErrorEmbed(Name, $"Challenges can only be sent in Ladder type tournaments. The tournament '{tournament.Name}' is of type '{tournament.Type}'.");
            }

            // Ensure tournament is running
            if (!tournament.IsRunning)
            {
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' is not currently running. Challenges can only be canceled in running tournaments.");
            }

            // Check if invoker is on challenger team (or guild admin)
            if (context.User is not SocketGuildUser guildUser)
            {
                return _embedManager.ErrorEmbed(Name, "Only members of the guild may use this command.");
            }
            // Store admin status for embed message use
            bool isGuildAdmin = guildUser.GuildPermissions.Administrator;
            // Check if user is on the challenger team
            if (!_teamManager.IsDiscordIdOnTeam(challengerTeam, context.User.Id) && !isGuildAdmin)
            {
                return _embedManager.ErrorEmbed(Name, $"You are not a member of the team '{challengerTeam.Name}', or an admin on this server, and cannot cancel a challenge on their behalf.");
            }

            // Check if there is a pending challenge between these two teams
            if (!_matchManager.HasChallengeSent(tournament, challengerTeamName))
            {
                return _embedManager.ErrorEmbed(Name, $"The team '{challengerTeam.Name}' does not have any pending challenges sent out to cancel.");
            }

            // Get the pending match
            var pendingMatch = _matchManager.GetChallengeMatchByChallengerName(tournament, challengerTeamName);
            if (pendingMatch == null)
            {
                // Shouldn't be possible, but just in case
                return _embedManager.ErrorEmbed(Name, "No pending match found. Canceling command.");
            }

            // Grab challenged team
            var challengedTeam = _teamManager.GetTeamByName(pendingMatch.Challenge.Challenged);
            if (challengedTeam == null)
            {
                // Shouldn't be possible, but just in case
                return _embedManager.ErrorEmbed(Name, $"The team '{pendingMatch.Challenge.Challenged}' was not found. Canceling command.");
            }

            // Reset challenge status on both teams
            challengerTeam.IsChallengeable = true;
            challengedTeam.IsChallengeable = true;

            // Cancel the challenge, remove match
            tournament.DeleteLadderMatchFromMatchLog(pendingMatch);

            // Save and reload the tournaments database
            _tournamentManager.SaveAndReloadTournamentsDatabase();
            
            // Backup to git repo
            _gitBackupManager.CopyAndBackupFilesToGit();
            
            return _embedManager.CancelChallengeSuccess(tournament, pendingMatch, isGuildAdmin);
        }
    }
}
