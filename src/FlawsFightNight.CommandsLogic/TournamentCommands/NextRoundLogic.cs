using Discord;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Interfaces;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.TournamentCommands
{
    public class NextRoundLogic : Logic
    {
        private EmbedManager _embedManager;
        private GitBackupManager _gitBackupManager;
        private MatchManager _matchManager;
        private TournamentManager _tournamentManager;
        public NextRoundLogic(EmbedManager embedManager, GitBackupManager gitBackupManager, MatchManager matchManager, TournamentManager tournamentManager) : base("Next Round")
        {
            _embedManager = embedManager;
            _gitBackupManager = gitBackupManager;
            _matchManager = matchManager;
            _tournamentManager = tournamentManager;
        }

        public Embed NextRoundProcess(string tournamentId)
        {
            // Check if the tournament exists, grab it if so
            if (!_tournamentManager.IsTournamentIdInDatabase(tournamentId))
            {
                return _embedManager.ErrorEmbed(Name, $"No tournament found with ID: {tournamentId}. Please check the ID and try again.");
            }
            var tournament = _tournamentManager.GetNewTournamentById(tournamentId);

            if (!tournament.IsRunning)
            {
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' is not currently running.");
            }

            // Check if the tournament is IRoundBased
            if (tournament is not IRoundBased roundBasedTournament)
            {
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' is not a round-based tournament and does not support round mechanics.");
            }
            else
            {
                if (!roundBasedTournament.CanAdvanceRound())
                {
                    return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' cannot advance to the next round at this time. Ensure all matches are completed and there is another round to advance to.");
                }

                // Advance to next round
                roundBasedTournament.AdvanceRound();

                // Save and reload the tournament database
                _tournamentManager.SaveAndReloadTournamentsDatabase();

                // Backup to git repo
                _gitBackupManager.CopyAndBackupFilesToGit();

                return _embedManager.NextRoundSuccess(tournament, roundBasedTournament.CurrentRound);
            }
        }
    }
}
