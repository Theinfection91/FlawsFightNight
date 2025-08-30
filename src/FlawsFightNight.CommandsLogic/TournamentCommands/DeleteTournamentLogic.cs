using Discord;
using FlawsFightNight.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.TournamentCommands
{
    public class DeleteTournamentLogic : Logic
    {
        private EmbedManager _embedManager;
        private TournamentManager _tournamentManager;

        public DeleteTournamentLogic(EmbedManager embedManager, TournamentManager tournamentManager) : base("Delete Tournament")
        {
            _embedManager = embedManager;
            _tournamentManager = tournamentManager;
        }
        public Embed DeleteTournamentProcess(string tournamentId)
        {
            // TODO Implement Delete Tournament Logic
            return _embedManager.ToDoEmbed("Success TODO");
        }
    }
}
