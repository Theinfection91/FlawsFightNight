using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFights.Data.DataModels
{
    public class DiscordCredentialFile
    {
        public string DiscordBotToken { get; set; } = "ENTER_BOT_TOKEN_HERE";
        public ulong GuildId { get; set; } = 0;
        public string CommandPrefix { get; set; } = "/";

        public DiscordCredentialFile() { }
    }
}
