using Discord;
using Discord.Interactions;
using FlawsFightNight.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.TeamCommands
{
    public class RegisterTeamLogic : Logic
    {
        private TournamentManager _tournamentManager;
        private TeamManager _teamManager;

        public RegisterTeamLogic(TournamentManager tournamentManager, TeamManager teamManager) : base("Register Team")
        {
            _tournamentManager = tournamentManager;
            _teamManager = teamManager;
        }

        public string RegisterTeamProcess(SocketInteractionContext context, string teamName, string tournamentId, List<IUser> members)
        {
            return "TODO";
        }
    }
}
