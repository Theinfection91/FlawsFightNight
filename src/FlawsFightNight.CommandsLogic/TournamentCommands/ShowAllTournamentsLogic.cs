using Discord;
using FlawsFightNight.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.TournamentCommands
{
    public class ShowAllTournamentsLogic : Logic
    {
        private EmbedManager _embedManager;
        private TournamentManager _tournamentManager;

        public ShowAllTournamentsLogic(EmbedManager embedManager, TournamentManager tournamentManager) : base("Show All Tournaments")
        {
            _embedManager = embedManager;
            _tournamentManager = tournamentManager;
        }

        public Embed ShowAllTournamentsProcess()
        {
            var tournaments = _tournamentManager.GetAllTournaments();
            if (tournaments == null || !tournaments.Any())
            {
                return _embedManager.ErrorEmbed(Name, "There are currently no tournaments created.");
            }

            return _embedManager.ShowAllTournamentsSuccess(tournaments);
        }
    }
}
