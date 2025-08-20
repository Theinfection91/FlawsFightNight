using Discord;
using FlawsFightNight.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.SettingsCommands
{
    public class RemoveMatchesChannelLogic : Logic
    {
        private EmbedManager _embedManager;
        private TournamentManager _tournamentManager;
        public RemoveMatchesChannelLogic(EmbedManager embedManager, TournamentManager tournamentManager) : base("Remove Matches Channel")
        {
            _embedManager = embedManager;
            _tournamentManager = tournamentManager;
        }
        public Embed RemoveMatchesChannelProcess(string tournamentId)
        {
            // Check if the tournament exists, grab it if so
            if (!_tournamentManager.IsTournamentIdInDatabase(tournamentId))
            {
                return _embedManager.ErrorEmbed(Name, $"No tournament found with ID: {tournamentId}. Please check the ID and try again.");
            }
            var tournament = _tournamentManager.GetTournamentById(tournamentId);
            // Remove the matches channel
            tournament.MatchesChannelId = 0;
            // Save and reload the tournaments database
            _tournamentManager.SaveAndReloadTournamentsDatabase();

            // TODO Create a success embed for this
            return _embedManager.ToDoEmbed($"Need to build correct embed, but executed the method and set {tournament.Name}'s Matches Channel ID to {tournament.MatchesChannelId}");
        }
    }
}
