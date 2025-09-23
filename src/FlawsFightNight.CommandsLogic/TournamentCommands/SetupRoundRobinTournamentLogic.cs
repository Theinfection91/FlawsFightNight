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

        public Embed SetupRoundRobinTournamentProcess(string tournamentId, RoundRobinMatchType roundRobinMatchType, TieBreakerType tieBreakerType, RoundRobinLengthType roundRobinType)
        {
            if (!_tournamentManager.IsTournamentIdInDatabase(tournamentId))
            {
                return _embedManager.ErrorEmbed(Name, $"No tournament found with ID: {tournamentId}. Please check the ID and try again.");
            }

            // Grab the tournament
            var tournament = _tournamentManager.GetTournamentById(tournamentId);

            // Ensure it is a round robin tournament
            if (tournament.Type.Equals(TournamentType.RoundRobin))
            {
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' is not a Round Robin tournament. This command can only be used for Round Robin tournaments.");
            }

            if (tournament.IsRunning)
            {
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' is already running. You cannot change its settings now.");
            }

            // Change match type to choosen type
            switch (roundRobinMatchType)
            {
                case RoundRobinMatchType.Normal:
                    tournament.RoundRobinMatchType = RoundRobinMatchType.Normal;
                    break;
                case RoundRobinMatchType.Open:
                    tournament.RoundRobinMatchType = RoundRobinMatchType.Open;
                    break;
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
                case RoundRobinLengthType.Single:
                    tournament.IsDoubleRoundRobin = false;
                    break;
                case RoundRobinLengthType.Double:
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
