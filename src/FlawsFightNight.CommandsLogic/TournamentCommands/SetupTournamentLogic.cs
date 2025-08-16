using Discord;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Core.Models.TieBreakers;
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

        public Embed SetupTournamentProcess(string tournamentId, TieBreakerType tieBreakerType, RoundRobinType roundRobinType)
        {
            if (!_tournamentManager.IsTournamentIdInDatabase(tournamentId))
            {
                return _embedManager.ErrorEmbed("Setup Tournament", $"No tournament found with ID: {tournamentId}. Please check the ID and try again.");
            }

            // Grab the tournament
            var tournament = _tournamentManager.GetTournamentById(tournamentId);

            if (tournament.IsRunning)
            {
                return _embedManager.ErrorEmbed("Setup Tournament", $"The tournament '{tournament.Name}' is already running. You cannot change its settings now.");
            }

            // Change tie breaker logic to choosen type
            switch (tieBreakerType)
            {
                case TieBreakerType.Traditional:
                    tournament.TieBreakerRule = new TraditionalTieBreaker();
                    break;
            }

            // Change round robin type
           switch (roundRobinType)
            {
                case RoundRobinType.Single:
                    tournament.IsDoubleRoundRobin = false;
                    break;
                case RoundRobinType.Double:
                    tournament.IsDoubleRoundRobin = true;
                    break;
            }

            // Save and reload the tournament database
            _tournamentManager.SaveAndReloadTournamentsDatabase();

            return _embedManager.SetupTournamentResolver(tournament);
        }
    }
}
