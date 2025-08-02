using Discord.Interactions;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.TournamentCommands
{
    public class LockTeamsLogic : Logic
    {
        private TournamentManager _tournamentManager;
        private TeamManager _teamManager;
        public LockTeamsLogic(TeamManager teamManager, TournamentManager tournamentManager) : base("Lock Teams")
        {
            _teamManager = teamManager;
            _tournamentManager = tournamentManager;
        }

        public string LockTeamsProcess(SocketInteractionContext context, string tournamentId)
        {
            // Check if the tournament exists, grab it if so
            if (!_tournamentManager.IsTournamentIdInDatabase(tournamentId))
            {
                return $"No tournament found with ID: {tournamentId}. Please check the ID and try again.";
            }
            Tournament? tournament = _tournamentManager.GetTournamentById(tournamentId);

            // Check type of tournament, ladder tournaments do not lock teams, so return an error message
            if (tournament.Type == Core.Enums.TournamentType.Ladder)
            {
                return $"The tournament '{tournament.Name}' is a Ladder tournament and does not require locking teams.";
            }

            // Check if the tournament is already locked
            if (tournament.IsTeamsLocked)
            {
                return $"The teams in the tournament '{tournament.Name}' are already locked.";
            }

            // Check if tournament can be locked. Tournaments need a minimum number of teams to be locked depending on the type of tournament.
            if (!tournament.CanTeamsBeLocked)
            {
                return $"The tournament '{tournament.Name}' cannot be locked at this time. Please ensure it has enough teams and is not running.";
            }

            // Lock the teams in the tournament
            _tournamentManager.LockTeamsInTournament(tournament);

            // Save and reload the tournament database
            _tournamentManager.SaveAndReloadTournamentsDatabase();

            // Build advanced return message
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Teams in the tournament '{tournament.Name}' have been successfully locked.");
            sb.AppendLine($"Tournament ID: {tournament.Id}");
            sb.AppendLine($"Team Size: {tournament.TeamSizeFormat}");
            sb.AppendLine($"Teams Locked: {tournament.IsTeamsLocked}");
            sb.AppendLine($"Can Teams Be Unlocked: {tournament.CanTeamsBeUnlocked}");
            return sb.ToString();
        }
    }
}
