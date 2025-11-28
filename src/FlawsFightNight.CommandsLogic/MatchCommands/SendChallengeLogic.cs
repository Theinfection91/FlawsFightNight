using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Core.Models.Tournaments;
using FlawsFightNight.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.MatchCommands
{
    public class SendChallengeLogic : Logic
    {
        private EmbedManager _embedManager;
        private GitBackupManager _gitBackupManager;
        private MatchManager _matchManager;
        private TeamManager _teamManager;
        private TournamentManager _tournamentManager;
        public SendChallengeLogic(EmbedManager embedManager, GitBackupManager gitBackupManager, MatchManager matchManager, TeamManager teamManager, TournamentManager tournamentManager) : base("Send Challenge")
        {
            _embedManager = embedManager;
            _gitBackupManager = gitBackupManager;
            _matchManager = matchManager;
            _teamManager = teamManager;
            _tournamentManager = tournamentManager;
        }

        public Embed SendChallengeProcess(SocketInteractionContext context, string challengerTeamName, string challengedTeamName)
        {
            if (!_teamManager.DoesTeamExist(challengerTeamName))
            {
                return _embedManager.ErrorEmbed(Name, $"No team found with the name '{challengerTeamName}'. Please check the name and try again.");
            }
            if (!_teamManager.DoesTeamExist(challengedTeamName))
            {
                return _embedManager.ErrorEmbed(Name, $"No team found with the name '{challengedTeamName}'. Please check the name and try again.");
            }

            // Grab teams
            Team? challengerTeam = _teamManager.GetTeamByName(challengerTeamName);
            Team? challengedTeam = _teamManager.GetTeamByName(challengedTeamName);

            // Grab tournament from challenger team
            var tournament = _tournamentManager.GetTournamentFromTeamName(challengerTeam.Name);
            if (tournament == null)
            {
                // Shouldn't be possible, but just in case
                return _embedManager.ErrorEmbed(Name, "Tournament is null. Contact support.");
            }

            // Ensure tournament is a ladder type
            if (tournament is not NormalLadderTournament)
            {
                return _embedManager.ErrorEmbed(Name, $"Challenges can only be sent in Ladder type tournaments. The tournament '{tournament.Name}' is of type '{tournament.Type}'.");
            }

            // Ensure tournament is running
            if (!tournament.IsRunning)
            {
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' is not currently running. Challenges can only be sent in running tournaments.");
            }

            // Ensure teams are in the same tournament
            if (!_tournamentManager.IsTeamsInSameTournament(tournament, challengerTeam, challengedTeam))
            {
                return _embedManager.ErrorEmbed(Name, $"Both teams must be registered in the same tournament to issue a challenge. Please check the team registrations and try again.");
            }

            // Ensure challenger team is not the same as challenged team
            if (challengerTeam.Name.Equals(challengedTeam.Name, StringComparison.OrdinalIgnoreCase))
            {
                return _embedManager.ErrorEmbed(Name, "A team cannot challenge itself. Please choose a different team to challenge.");
            }

            // Challenger cannot be higher rank than challenged team
            if (challengerTeam.Rank < challengedTeam.Rank)
            {
                return _embedManager.ErrorEmbed(Name, $"The challenger team (#{challengerTeam.Rank}){challengerTeam.Name} is ranked higher than the challenged team (#{challengedTeam.Rank}){challengedTeam.Name} - In a ladder tournament, a team may only challenge another team that is ranked higher than itself.");
            }

            // Ensure challenged team is only 2 ranks above challenger team at most
            if (!_matchManager.IsChallengedTeamWithinRanks(challengerTeam, challengedTeam))
            {
                return _embedManager.ErrorEmbed(Name, $"The challenger team (#{challengerTeam.Rank}){challengerTeam.Name} can not challenge (#{challengedTeam.Rank}){challengedTeam.Name} - Teams may only challenge up to 2 ranks above them.");
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
                return _embedManager.ErrorEmbed(Name, $"You are not a member of the team '{challengerTeam.Name}', or an admin on this server, and cannot issue a challenge on their behalf.");
            }

            // Check each teams challenge status, ensure neither are already in a challenge situation (Challenge sent or received and awaiting to be played)
            if (!challengerTeam.IsChallengeable)
            {
                var existingChallenge = _matchManager.GetOpenMatchByTeamNameLadder(tournament, challengerTeam.Name);
                if (challengerTeam.Name.Equals(existingChallenge.Challenge.Challenger))
                {
                    return _embedManager.ErrorEmbed(Name, $"The team '{challengerTeam.Name}' has already sent a challenge to '{existingChallenge.Challenge.Challenged}' and is awaiting the match to be played.");
                }
                else
                {
                    return _embedManager.ErrorEmbed(Name, $"The team '{challengerTeam.Name}' has already been challenged by '{existingChallenge.Challenge.Challenger}' and is awaiting the match to be played first.");
                }
            }
            if (!challengedTeam.IsChallengeable)
            {
                var existingChallenge = _matchManager.GetOpenMatchByTeamNameLadder(tournament, challengedTeam.Name);
                if (challengedTeam.Name.Equals(existingChallenge.Challenge.Challenger))
                {
                    return _embedManager.ErrorEmbed(Name, $"The team '{challengedTeam.Name}' has already sent a challenge to '{existingChallenge.Challenge.Challenged}' and is awaiting the match to be played.");
                }
                else
                {
                    return _embedManager.ErrorEmbed(Name, $"The team '{challengedTeam.Name}' has already been challenged by '{existingChallenge.Challenge.Challenger}' and is awaiting the match to be played first.");
                }
            }

            // Create new challenge match
            var newChallengeMatch = _matchManager.CreateLadderMatchWithChallenge(challengerTeam, challengedTeam);

            // Add the new match to the tournament
            tournament.MatchLog.AddMatch(newChallengeMatch);

            // Update both teams challengeable status
            challengerTeam.IsChallengeable = false;
            challengedTeam.IsChallengeable = false;

            // Send direct message notifications to each member of both teams about the challenge
            _matchManager.SendChallengeSuccessNotificationProcess(tournament, newChallengeMatch, challengerTeam, challengedTeam);

            // Save and reload the tournament database
            _tournamentManager.SaveAndReloadTournamentsDatabase();

            // Backup to git repo
            _gitBackupManager.CopyAndBackupFilesToGit();

            return _embedManager.SendChallengeSuccess(tournament, newChallengeMatch, isGuildAdmin);
        }
    }
}
