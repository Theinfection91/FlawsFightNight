using Discord;
using FlawsFightNight.Commands;
using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.TournamentCommands
{
    public class ShowAllTournamentsHandler : CommandHandler
    {
        private EmbedFactory _embedFactory;
        private TournamentService _tournamentService;

        public ShowAllTournamentsHandler(EmbedFactory embedFactory, TournamentService tournamentService) : base("Show All Tournaments")
        {
            _embedFactory = embedFactory;
            _tournamentService = tournamentService;
        }

        public Embed ShowAllTournamentsProcess()
        {
            var tournaments = _tournamentService.GetAllTournaments();
            if (tournaments == null || !tournaments.Any())
            {
                return _embedFactory.ErrorEmbed(Name, "There are currently no tournaments created.");
            }

            return _embedFactory.ShowAllTournamentsSuccess(tournaments);
        }
    }
}
