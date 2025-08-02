using Discord.Interactions;
using FlawsFightNight.Core.Enums;
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

        public string CreateTournamentProcess(SocketInteractionContext context, string name, TournamentType tournamentType, string? description = null)
        {
            return "Hello";
        }
    }
}
