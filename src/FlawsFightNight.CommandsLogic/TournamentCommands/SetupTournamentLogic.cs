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
    public class SetupTournamentLogic : Logic
    {
        private EmbedManager _embedManager;
        private GitBackupManager _gitBackupManager;
        private TournamentManager _tournamentManager;

        public SetupTournamentLogic(EmbedManager embedManager, GitBackupManager gitBackupManager, TournamentManager tournamentManager) : base("Setup Tournament")
        {
            _embedManager = embedManager;
            _gitBackupManager = gitBackupManager;
            _tournamentManager = tournamentManager;
        }

        public Embed SetupTournamentProcess(string tournamentId, TieBreakerType tieBreakerType, RoundRobinType roundRobinType)
        {
            // Check if the tournament exists, grab it if so
            if (!_tournamentManager.IsTournamentIdInDatabase(tournamentId))
            {
                return _embedManager.ErrorEmbed(Name, $"No tournament found with ID: {tournamentId}. Please check the ID and try again.");
            }
            var tournament = _tournamentManager.GetTournamentById(tournamentId);

            // Check if tournament is Round Robin or not (only Round Robin supported for now)
            if (!tournament.Type.Equals(TournamentType.RoundRobin))
            {
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' is not a Round Robin tournament. Only Round Robin tournaments can have their settings changed at the moment.");
            }

            // Check if tournament is already running
            if (tournament.IsRunning)
            {
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' is already running. You cannot change its settings now.");
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

            // Backup to git repo
            _gitBackupManager.CopyAndBackupFilesToGit();

            return _embedManager.SetupTournamentResolver(tournament);
        }
    }
}
