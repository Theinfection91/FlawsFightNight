using Discord;
using Discord.Interactions;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Core.Models.Tournaments;
using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.TournamentCommands
{
    public class CreateTournamentLogic : Logic
    {
        private EmbedFactory _embedManager;
        private GitBackupService _gitBackupManager;
        private TournamentService _tournamentManager;

        public CreateTournamentLogic(EmbedFactory embedManager, GitBackupService gitBackupManager, TournamentService tournamentManager) : base("Create Tournament")
        {
            _embedManager = embedManager;
            _gitBackupManager = gitBackupManager;
            _tournamentManager = tournamentManager;
        }

        public async Task<Embed> CreateTournamentProcess(SocketInteractionContext context, string name, TournamentType tournamentType, int teamSize, string? description = null)
        {
            // Check if tournament name is unique
            if (!_tournamentManager.IsTournamentNameUnique(name))
            {
                return _embedManager.ErrorEmbed(Name, $"A tournament with the name '{name}' already exists. Please choose a different name.");
            }

            // Create the tournament
            Tournament tournament = _tournamentManager.CreateNewTournament(name, tournamentType, teamSize, description);

            // Prevent any tournament types that are not Round Robin or Ladder for now
            if (tournament.Type is not (TournamentType.DSRLadder or TournamentType.NormalLadder or TournamentType.NormalRoundRobin or TournamentType.OpenRoundRobin))
            {
                return _embedManager.ToDoEmbed("Sorry, but for now only DSR Ladder, Normal Ladder, and either Normal or Open Round Robin tournaments may be created and played. Please try again.");
            }

            // Save and reload the database
            await _tournamentManager.SaveAndReloadTournamentDataFiles(tournament);

            // Backup to git repo
            _gitBackupManager.EnqueueBackup();

            return _embedManager.CreateTournamentSuccessResolver(tournament);
        }
    }
}
