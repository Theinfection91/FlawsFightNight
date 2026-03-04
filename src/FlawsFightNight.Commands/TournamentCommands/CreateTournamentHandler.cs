using Discord;
using Discord.Interactions;
using FlawsFightNight.Commands;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Core.Models.Tournaments;
using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.TournamentCommands
{
    public class CreateTournamentHandler : CommandHandler
    {
        private EmbedFactory _embedFactory;
        private GitBackupService _gitBackupService;
        private TournamentService _tournamentService;

        public CreateTournamentHandler(EmbedFactory embedFactory, GitBackupService gitBackupService, TournamentService tournamentService) : base("Create Tournament")
        {
            _embedFactory = embedFactory;
            _gitBackupService = gitBackupService;
            _tournamentService = tournamentService;
        }

        public async Task<Embed> CreateTournamentProcess(SocketInteractionContext context, string name, TournamentType tournamentType, int teamSize, string? description = null)
        {
            // Check if tournament name is unique
            if (!_tournamentService.IsTournamentNameUnique(name))
            {
                return _embedFactory.ErrorEmbed(Name, $"A tournament with the name '{name}' already exists. Please choose a different name.");
            }

            // Create the tournament
            Tournament tournament = _tournamentService.CreateNewTournament(name, tournamentType, teamSize, description);

            // Prevent any tournament types that are not Round Robin or Ladder for now
            if (tournament.Type is not (TournamentType.DSRLadder or TournamentType.NormalLadder or TournamentType.NormalRoundRobin or TournamentType.OpenRoundRobin))
            {
                return _embedFactory.ToDoEmbed("Sorry, but for now only DSR Ladder, Normal Ladder, and either Normal or Open Round Robin tournaments may be created and played. Please try again.");
            }

            // Save and reload the database
            await _tournamentService.SaveAndReloadTournamentDataFiles(tournament);

            // Backup to git repo
            _gitBackupService.EnqueueBackup();

            return _embedFactory.CreateTournamentSuccessResolver(tournament);
        }
    }
}
