using Discord;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Interfaces;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Core.Models.TieBreakers;
using FlawsFightNight.Core.Models.Tournaments;
using FlawsFightNight.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.TournamentCommands
{
    public class SetupRoundRobinTournamentLogic : Logic
    {
        private EmbedManager _embedManager;
        private GitBackupManager _gitBackupManager;
        private TournamentManager _tournamentManager;

        public SetupRoundRobinTournamentLogic(EmbedManager embedManager, GitBackupManager gitBackupManager, TournamentManager tournamentManager) : base("Setup Round Robin Tournament")
        {
            _embedManager = embedManager;
            _gitBackupManager = gitBackupManager;
            _tournamentManager = tournamentManager;
        }

        public Embed SetupRoundRobinTournamentProcess(string tournamentId, TieBreakerType tieBreakerType, RoundRobinLengthType roundRobinType)
        {
            // Check if the tournament exists, grab it if so
            if (!_tournamentManager.IsTournamentIdInDatabase(tournamentId))
            {
                return _embedManager.ErrorEmbed(Name, $"No tournament found with ID: {tournamentId}. Please check the ID and try again.");
            }

            // Grab the tournament
            var tournament = _tournamentManager.GetTournamentById(tournamentId);
            if (tournament == null)
            {
                return _embedManager.ErrorEmbed(Name, "An error occurred while retrieving the tournament. Contact support.");
            }

            // Ensure it is a form of round robin tournament
            if (tournament is not NormalRoundRobinTournament or OpenRoundRobinTournament)
            {
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' is not a Normal or Open Round Robin Tournament. This command can only be used for Round Robin tournaments.");
            }

            if (tournament.IsRunning)
            {
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' is already running. You cannot change it's settings now.");
            }

            // Change tie breaker logic to chosen type
            if (tournament is ITieBreakerRankSystem tbTournament)
                switch (tieBreakerType)
                {
                    case TieBreakerType.Traditional:
                        tbTournament.TieBreakerRule = new TraditionalTieBreaker();
                        break;
                }

            // Change round robin type
            if (tournament is IRoundRobinLength rrTournament)
                switch (roundRobinType)
            {
                case RoundRobinLengthType.Single:
                    rrTournament.IsDoubleRoundRobin = false;
                    break;
                case RoundRobinLengthType.Double:
                    rrTournament.IsDoubleRoundRobin = true;
                    break;
            }

            // Save and reload the tournament database
            _tournamentManager.SaveAndReloadTournamentsDatabase();

            // Backup to git repo
            _gitBackupManager.CopyAndBackupFilesToGit();

            return _embedManager.RoundRobinSetupTournamentSuccess(tournament);
        }
    }
}
