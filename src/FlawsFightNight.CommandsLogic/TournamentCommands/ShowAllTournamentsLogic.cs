using Discord;
using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.TournamentCommands
{
    public class ShowAllTournamentsLogic : Logic
    {
        private EmbedFactory _embedManager;
        private TournamentService _tournamentManager;

        public ShowAllTournamentsLogic(EmbedFactory embedManager, TournamentService tournamentManager) : base("Show All Tournaments")
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
