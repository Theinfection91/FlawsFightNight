using Discord;
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
    public class DeleteTournamentLogic : Logic
    {
        private EmbedManager _embedManager;
        private TournamentManager _tournamentManager;

        public DeleteTournamentLogic(EmbedManager embedManager, TournamentManager tournamentManager) : base("Delete Tournament")
        {
            _embedManager = embedManager;
            _tournamentManager = tournamentManager;
        }

        public Embed DeleteTournamentProcess(string tournamentId)
        {
            var tournament = _tournamentManager.GetTournamentById(tournamentId);
            if (tournament.IsRunning)
            {
                return _embedManager.ErrorEmbed(Name, "Cannot delete a tournament that is currently running. End the tournament and try again.");
            }
            switch (tournament.Type)
            {
                case TournamentType.RoundRobin:
                    return RoundRobinDeleteTournament(tournament);
            }
            return _embedManager.ErrorEmbed(Name, "Tournament type not supported for deletion yet.");
        }

        public Embed RoundRobinDeleteTournament(Tournament tournament)
        {
            if (!tournament.IsRunning && tournament.IsTeamsLocked)
            {
                return _embedManager.ErrorEmbed(Name, "Even though the tournament is not running, the teams are still locked. Unlock teams first if you want to delete this tournament. Once started, it cannot be deleted until it is ended.");
            }

            _tournamentManager.DeleteTournament(tournament.Id);

            return _embedManager.DeleteTournamentSuccess(tournament);
        }
    }
}
