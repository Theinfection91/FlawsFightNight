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

            Tournament tournament = _tournamentManager.CreateTournament(name, tournamentType, teamSize, description);

            // Prevent any tournament types that are not Round Robin or Ladder for now
            if (!tournament.Type.Equals(TournamentType.RoundRobin) && !tournament.Type.Equals(TournamentType.Ladder))
            {
                return _embedManager.ToDoEmbed("Sorry, but for now only Round Robin or Ladder tournaments may be created and played. Please try again.");
            }

            if (tournament == null)
            {
                return _embedManager.ErrorEmbed(Name, "Null tournament returned. Canceling command.");
            }

            // Add the tournament, this will also save and reload the database
            _tournamentManager.AddTournament(tournament);

            // Backup to git repo
            _gitBackupManager.CopyAndBackupFilesToGit();

            return _embedManager.CreateTournamentSuccessResolver(tournament);
        }
    }
}
