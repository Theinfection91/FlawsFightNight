using Discord;
using Discord.Interactions;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Core.Models.Tournaments;
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
        private GitBackupManager _gitBackupManager;
        private TournamentManager _tournamentManager;

        public CreateTournamentLogic(EmbedManager embedManager, GitBackupManager gitBackupManager, TournamentManager tournamentManager) : base("Create Tournament")
        {
            _embedManager = embedManager;
            _gitBackupManager = gitBackupManager;
            _tournamentManager = tournamentManager;
        }

        public Embed CreateTournamentProcess(SocketInteractionContext context, string name, TournamentType tournamentType, int teamSize, string? description = null)
        {
            // Check if tournament name is unique
            if (!_tournamentManager.IsTournamentNameUnique(name))
            {
                return _embedManager.ErrorEmbed(Name, $"A tournament with the name '{name}' already exists. Please choose a different name.");
            }

            // New version
            TournamentBase tournament = _tournamentManager.CreateNewTournament(name, tournamentType, teamSize, description);

            if (tournament == null)
            {
                return _embedManager.ErrorEmbed(Name, "Null tournament returned. Canceling command. Contact an admin for support.");
            }

            // Prevent any tournament types that are not Round Robin or Ladder for now
            if (tournament.Type is not (TournamentType.NormalLadder or TournamentType.NormalRoundRobin or TournamentType.OpenRoundRobin))
            {
                return _embedManager.ToDoEmbed("Sorry, but for now only Normal Ladder and either Normal or Open Round Robin tournaments may be created and played. Please try again.");
            }

            // Add the tournament
            _tournamentManager.AddTournament(tournament);

            // Save and reload the database
            _tournamentManager.SaveAndReloadTournamentsDatabase();

            // Backup to git repo
            _gitBackupManager.CopyAndBackupFilesToGit();

            return _embedManager.CreateTournamentSuccessResolver(tournament);
        }
    }
}
