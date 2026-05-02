using Discord;
using FlawsFightNight.Commands;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Interfaces;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Core.Models.TieBreakers;
using FlawsFightNight.Core.Models.Tournaments;
using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.TournamentCommands
{
    public class SetupRoundRobinTournamentHandler : CommandHandler
    {
        private EmbedFactory _embedFactory;
        private GitBackupService _gitBackupService;
        private TournamentService _tournamentService;

        public SetupRoundRobinTournamentHandler(EmbedFactory embedFactory, GitBackupService gitBackupService, TournamentService tournamentService) : base("Setup Round Robin Tournament")
        {
            _embedFactory = embedFactory;
            _gitBackupService = gitBackupService;
            _tournamentService = tournamentService;
        }

        public async Task<Embed> SetupRoundRobinTournamentProcess(string tournamentId, TieBreakerType tieBreakerType, RoundRobinLengthType roundRobinType)
        {
            // Check if the tournament exists, grab it if so
            if (!_tournamentService.IsTournamentIdInDatabase(tournamentId))
            {
                return _embedFactory.ErrorEmbed(Name, $"No tournament found with ID: {tournamentId}. Please check the ID and try again.");
            }

            // Grab the tournament
            var tournament = _tournamentService.GetTournamentById(tournamentId);
            if (tournament == null)
            {
                return _embedFactory.ErrorEmbed(Name, "An error occurred while retrieving the tournament. Contact support.");
            }

            // Ensure it is a form of round robin tournament
            if (tournament is not NormalRoundRobinTournament and not OpenRoundRobinTournament)
            {
                return _embedFactory.ErrorEmbed(Name, $"The tournament '{tournament.Name}' is not a Normal or Open Round Robin Tournament. This command can only be used for Round Robin tournaments.");
            }

            if (tournament.IsRunning)
            {
                return _embedFactory.ErrorEmbed(Name, $"The tournament '{tournament.Name}' is already running. You cannot change it's settings now.");
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
            await _tournamentService.SaveAndReloadTournamentDataFiles(tournament);

            // Backup to git repo
            _gitBackupService.EnqueueBackup();

            return _embedFactory.RoundRobinSetupTournamentSuccess(tournament);
        }
    }
}
