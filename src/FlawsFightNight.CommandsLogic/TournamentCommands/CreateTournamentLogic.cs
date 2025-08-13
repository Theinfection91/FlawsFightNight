using Discord;
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
        private EmbedManager _embedManager;
        private TournamentManager _tournamentManager;

        public CreateTournamentLogic(EmbedManager embedManager, TournamentManager tournamentManager) : base("Create Tournament")
        {
            _embedManager = embedManager;
            _tournamentManager = tournamentManager;
        }

        public Embed CreateTournamentProcess(SocketInteractionContext context, string name, TournamentType tournamentType, int teamSize, string? description = null)
        {
            // Check if tournament name is unique
            if (!_tournamentManager.IsTournamentNameUnique(name))
            {
                return _embedManager.ErrorEmbed(Name, $"A tournament with the name '{name}' already exists. Please choose a different name.");
            }

            Tournament tournament = _tournamentManager.CreateTournament(name, tournamentType, teamSize, description);
            if (tournament == null)
            {
                return _embedManager.ErrorEmbed("Null", "Invalid tournament type specified.");
            }
            _tournamentManager.AddTournament(tournament);
            return _embedManager.CreateTournamentSuccessResolver(tournament);
        }
    }
}
