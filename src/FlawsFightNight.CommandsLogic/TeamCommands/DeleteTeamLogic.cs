﻿using Discord;
using FlawsFightNight.Managers;
using FlawsFightNight.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlawsFightNight.Core.Models;

namespace FlawsFightNight.CommandsLogic.TeamCommands
{
    public class DeleteTeamLogic : Logic
    {
        private EmbedManager _embedManager;
        private GitBackupManager _gitBackupManager;
        private MatchManager _matchManager;
        private TeamManager _teamManager;
        private TournamentManager _tournamentManager;

        public DeleteTeamLogic(EmbedManager embedManager, GitBackupManager gitBackupManager, MatchManager matchManager, TeamManager teamManager, TournamentManager tournamentManager) : base("Remove Team")
        {
            _embedManager = embedManager;
            _gitBackupManager = gitBackupManager;
            _matchManager = matchManager;
            _teamManager = teamManager;
            _tournamentManager = tournamentManager;
        }

        public Embed DeleteTeamProcess(string teamName)
        {
            // Grab tournament from team name
            var tournament = _tournamentManager.GetTournamentFromTeamName(teamName);

            // Send to specific logic based on tournament type
            switch (tournament.Type)
            {
                case TournamentType.Ladder:
                    return LadderDeleteTeamProcess(tournament, teamName);
                case TournamentType.RoundRobin:
                    return RoundRobinDeleteTeamProcess(tournament, teamName);
                default:
                    return _embedManager.ErrorEmbed(Name, "Tournament type not supported for team deletion yet.");
            }
        }

        public Embed LadderDeleteTeamProcess(Tournament tournament, string teamName)
        {
            /* Teams can be removed at any time in ladder tournaments, even after starting */

            // Grab team object for embed
            var team = _teamManager.GetTeamByName(teamName);

            // Check if team was involved in a challenge, delete if so and reset challenge status of other team
            if (_matchManager.IsMatchMadeForTeamResolver(tournament, teamName))
            {
                // Open variable to use, grab the match
                Team teamToEdit;
                var match = _matchManager.GetLadderMatchForTeam(tournament, teamName);

                // If team to delete is challenger, grab challenged team and edit
                if (match.Challenge.Challenger.Equals(teamName, StringComparison.OrdinalIgnoreCase))
                {
                    teamToEdit = _teamManager.GetTeamByName(match.Challenge.Challenged);
                    teamToEdit.IsChallengeable = true;
                }
                // If team to delete is challenged, grab challenger team and edit
                else if (match.Challenge.Challenged.Equals(teamName, StringComparison.OrdinalIgnoreCase))
                {
                    teamToEdit = _teamManager.GetTeamByName(match.Challenge.Challenger);
                    teamToEdit.IsChallengeable = true;
                }
                // Delete the match
                tournament.DeleteLadderMatchFromMatchLog(match);
            }

            // Remove the team from the tournament
            _teamManager.DeleteTeamFromDatabase(teamName);
            
            // Adjust ranks of remaining teams if necessary
            _matchManager.ReassignRanksInLadderTournament(tournament);

            // Run challenge rank comparison
            _matchManager.ChallengeRankComparisonProcess(tournament);

            // Save and reload the tournament database
            _tournamentManager.SaveAndReloadTournamentsDatabase();

            // Backup to git repo
            _gitBackupManager.CopyAndBackupFilesToGit();

            return _embedManager.TeamDeleteSuccess(team, tournament);
        }

        public Embed RoundRobinDeleteTeamProcess(Tournament tournament, string teamName)
        {
            // Check if tournament is locked for adding or removing teams
            if (tournament.IsTeamsLocked && !tournament.IsRunning)
            {
                return _embedManager.ErrorEmbed(Name, $"Teams have been locked, but the tournament has not started yet. An admin can unlock this before starting to remove or add teams as a last chance before beginning a round robin tournament.");
            }
            // Check if tournament is running, cant remove after starting only before locking
            if (tournament.IsRunning)
            {
                return _embedManager.ErrorEmbed(Name, $"Teams cannot be removed from a Round Robin tournament that is currently running. If a team can no longer participate then have their opponents report they have won with a score of 0 to 0.");
            }

            // Grab team object for embed
            var team = _teamManager.GetTeamByName(teamName);

            // Remove the team from the tournament
            _teamManager.DeleteTeamFromDatabase(teamName);

            // Adjust ranks of remaining teams
            tournament.SetRanksByTieBreakerLogic();

            // Save and reload the tournament database
            _tournamentManager.SaveAndReloadTournamentsDatabase();

            // Backup to git repo
            _gitBackupManager.CopyAndBackupFilesToGit();

            return _embedManager.TeamDeleteSuccess(team, tournament);
        }
    }
}
