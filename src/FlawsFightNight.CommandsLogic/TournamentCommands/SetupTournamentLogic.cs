using Discord;
using FlawsFightNight.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.TournamentCommands
{
    public class SetupTournamentLogic
    {
        private EmbedManager _embedManager;
        private TournamentManager _tournamentManager;

        public SetupTournamentLogic(EmbedManager embedManager, TournamentManager tournamentManager)
        {
            _embedManager = embedManager;
            _tournamentManager = tournamentManager;
        }

        public Embed SetupTournamentProcess(string tournamentId)
        {
            return _embedManager.ToDoEmbed();
        }
    }
}
