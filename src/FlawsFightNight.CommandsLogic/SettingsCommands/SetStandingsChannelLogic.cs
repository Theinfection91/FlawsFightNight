using Discord;
using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.SettingsCommands
{
    public class SetStandingsChannelLogic : Logic
    {
        private EmbedFactory _embedManager;
        private GitBackupService _gitBackupManager;
        private TournamentService _tournamentManager;

        public SetStandingsChannelLogic(EmbedFactory embedManager, GitBackupService gitBackupManager, TournamentService tournamentManager) : base("Set Standings Channel")
        {
            _embedManager = embedManager;
            _gitBackupManager = gitBackupManager;
            _tournamentManager = tournamentManager;
        }

        public async Task<Embed> SetStandingsChannelProcess(string tournamentId, IMessageChannel channel)
        {
            // Check if the tournament exists, grab it if so
            if (!_tournamentManager.IsTournamentIdInDatabase(tournamentId))
            {
                return _embedManager.ErrorEmbed(Name, $"No tournament found with ID: {tournamentId}. Please check the ID and try again.");
            }
            var tournament = _tournamentManager.GetTournamentById(tournamentId);

            tournament.StandingsChannelId = channel.Id;

            // Save and reload the tournaments database
            await _tournamentManager.SaveAndReloadTournamentDataFiles(tournament);

            // Backup to git repo
            _gitBackupManager.EnqueueBackup();

            return _embedManager.SetStandingsChannelSuccess(channel, tournament);
        }
    }
}
