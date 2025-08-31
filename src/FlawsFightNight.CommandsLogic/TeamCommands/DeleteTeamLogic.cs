using Discord;
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
        private TeamManager _teamManager;
        private TournamentManager _tournamentManager;

        public DeleteTeamLogic(EmbedManager embedManager, TeamManager teamManager, TournamentManager tournamentManager) : base("Remove Team")
        {
            _embedManager = embedManager;
            _teamManager = teamManager;
            _tournamentManager = tournamentManager;
        }

        public Embed DeleteTeamProcess(string teamName)
        {
            // Check if the team exists
            //if (_teamManager.IsTeamNameUnique(teamName))
            //{
            //    return _embedManager.ErrorEmbed(Name, $"No team found with the name: {teamName}. Please check the name and try again.");
            //}

            // Grab tournament from team name
            var tournament = _tournamentManager.GetTournamentFromTeamName(teamName);

            // Send to specific logic based on tournament type
            switch (tournament.Type)
            {
                case TournamentType.RoundRobin:
                    return RoundRobinDeleteTeamProcess(tournament, teamName);
            }
            return _embedManager.ToDoEmbed("Delete Team Logic Not Yet Implemented For Tournament Type");
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

            // Save and reload the tournament database
            _tournamentManager.SaveAndReloadTournamentsDatabase();

            return _embedManager.TeamDeleteSuccess(team, tournament);
        }
    }
}
