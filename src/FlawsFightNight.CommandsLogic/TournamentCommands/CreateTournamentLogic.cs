using Discord.Interactions;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.TournamentCommands
{
    public class CreateTournamentLogic : Logic
    {
        private TournamentManager _tournamentManager;

        public CreateTournamentLogic(TournamentManager tournamentManager) : base("Create Tournament")
        {
            _tournamentManager = tournamentManager;
        }

        public string CreateTournamentProcess(SocketInteractionContext context, string name, TournamentType tournamentType, int teamSize, string? description = null)
        {
            Tournament tournament = _tournamentManager.CreateSpecificTournament(name, tournamentType, teamSize, description);
            if (tournament == null)
            {
                return "Invalid tournament type specified.";
            }
            _tournamentManager.AddTournament(tournament);
            return $"A {tournament.TeamSize}v{tournament.TeamSize} {tournament.Type} Tournament named '{tournament.Name}' was created.";
        }
    }
}
