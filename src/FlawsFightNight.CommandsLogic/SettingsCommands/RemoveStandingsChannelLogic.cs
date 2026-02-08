using Discord;
using FlawsFightNight.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.SettingsCommands
{
    public class RemoveStandingsChannelLogic : Logic
    {
        private EmbedManager _embedManager;
        private GitBackupManager _gitBackupManager;
        private TournamentManager _tournamentManager;

        public RemoveStandingsChannelLogic(EmbedManager embedManager, GitBackupManager gitBackupManager, TournamentManager tournamentManager) : base("Remove Standings Channel")
        {
            _embedManager = embedManager;
            _gitBackupManager = gitBackupManager;
            _tournamentManager = tournamentManager;
        }

        public Embed RemoveStandingsChannelProcess(string tournamentId)
        {
            // Check if the tournament exists, grab it if so
            if (!_tournamentManager.IsTournamentIdInDatabase(tournamentId))
            {
                return _embedManager.ErrorEmbed(Name, $"No tournament found with ID: {tournamentId}. Please check the ID and try again.");
            }
            var tournament = _tournamentManager.GetTournamentById(tournamentId);

            // Check if a standings channel is set
            if (tournament.StandingsChannelId == 0)
            {
                return _embedManager.ErrorEmbed(Name, $"Tournament {tournament.Name} ({tournament.Id}) does not have a standings channel set.");
            }

            // Remove the standings channel
            tournament.StandingsChannelId = 0;
            tournament.StandingsMessageId = 0;

            // Save and reload the tournaments database
            _tournamentManager.SaveAndReloadTournamentDataFiles(tournament);

            // Backup to git repo
            _gitBackupManager.CopyAndBackupFilesToGit();

            return _embedManager.RemoveStandingsChannelSuccess(tournament);
        }
    }
}
