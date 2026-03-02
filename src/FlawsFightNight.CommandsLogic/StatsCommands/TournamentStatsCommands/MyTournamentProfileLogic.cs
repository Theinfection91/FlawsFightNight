using Discord;
using FlawsFightNight.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.StatsCommands.TournamentStatsCommands
{
    public class MyTournamentProfileLogic : Logic
    {
        private readonly EmbedManager _embedManager;
        public MyTournamentProfileLogic(EmbedManager embedManager) : base("My Tournament Profile")
        {
            _embedManager = embedManager;
        }

        public async Task<Embed> MyTournamentProfileProcess(ulong userId)
        {
            return _embedManager.ToDoEmbed("This feature is still in development. Stay tuned for updates!");
        }
    }
}