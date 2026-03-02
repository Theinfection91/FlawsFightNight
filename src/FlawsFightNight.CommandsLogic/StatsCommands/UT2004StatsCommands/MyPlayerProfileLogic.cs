using Discord;
using FlawsFightNight.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.StatsCommands.UT2004StatsCommands
{
    public class MyPlayerProfileLogic : Logic
    {
        private readonly EmbedManager _embedManager;
        public MyPlayerProfileLogic(EmbedManager embedManager) : base("My Player Profile")
        {
            _embedManager = embedManager;
        }

        public async Task<Embed> MyPlayerProfileProcess(ulong userId)
        {
            return _embedManager.ToDoEmbed("This feature is still in development. Stay tuned for updates!");
        }
    }
}
