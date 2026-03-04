using Discord;
using FlawsFightNight.Services;
using FlawsFightNight.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Core.Interfaces;

namespace FlawsFightNight.Commands.TeamCommands
{
    public class DeleteTeamLogic : CommandHandler
    {
        private EmbedFactory _embedFactory;
        private GitBackupService _gitBackupService;
        private MatchService _matchService;
        private TeamService _teamService;
        private TournamentService _tournamentService;

        public DeleteTeamLogic(EmbedFactory embedFactory, GitBackupService gitBackupService, MatchService matchService, TeamService teamService, TournamentService tournamentService) : base("Remove Team")
        {
            _embedFactory = embedFactory;
            _gitBackupService = gitBackupService;
            _matchService = matchService;
            _teamService = teamService;
            _tournamentService = tournamentService;
        }

        public async Task<Embed> DeleteTeamProcess(string teamName)
        {
            // Grab tournament from team name
            var tournament = _tournamentService.GetTournamentFromTeamName(teamName);

            // Handle team locking tournament conditions (Round Robin and eventually Elimination)
            if (tournament is ITeamLocking teamLockingTournament)
            {
                if (teamLockingTournament.IsTeamsLocked && !tournament.IsRunning)
                {
                    return _embedFactory.ErrorEmbed(Name, "Teams have been locked for this tournament, team deletion is not allowed.");
                }

                // Check if tournament is running, cant remove after starting only before locking
                if (tournament.IsRunning)
                {
                    return _embedFactory.ErrorEmbed(Name, $"Teams cannot be removed from a Round Robin tournament that is currently running. If a team can no longer participate then have their opponents report they have won with a score of 0 to 0.");
                }
            }

            // Grab team object for embed
            var team = tournament.GetTeam(teamName);

            // Handle ladder tournament challenge procedures
            if (tournament.MatchLog is IChallengeLog challengeLog)
            {
                // Check if team has a pending challenge
                if (challengeLog.HasPendingChallenge(team, out var challengeMatch))
                {
                    // Team to edit is challenged team, reset their challengeable status
                    if (challengeMatch.Challenge.Challenger.Equals(teamName, StringComparison.OrdinalIgnoreCase))
                    {
                        tournament.GetTeam(challengeMatch.Challenge.Challenged).IsChallengeable = true;
                    }
                    // Team to edit is challenger team, reset their challengeable status
                    else if (challengeMatch.Challenge.Challenged.Equals(teamName, StringComparison.OrdinalIgnoreCase))
                    {
                        tournament.GetTeam(challengeMatch.Challenge.Challenger).IsChallengeable = true;
                    }

                    // Remove the challenge match from the tournament
                    tournament.MatchLog.RemoveMatch(challengeMatch);
                }
            }

            // Remove the team
            tournament.RemoveTeam(team);

            // Adjust ranks
            tournament.AdjustRanks();

            // Save and reload the tournament database
            await _tournamentService.SaveAndReloadTournamentDataFiles(tournament);

            // Backup to git repo
            _gitBackupService.EnqueueBackup();

            return _embedFactory.TeamDeleteSuccess(team, tournament);
        }
    }
}
