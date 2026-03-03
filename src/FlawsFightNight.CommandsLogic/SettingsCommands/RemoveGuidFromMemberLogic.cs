using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.SettingsCommands
{
    public class RemoveGuidFromMemberLogic : Logic
    {
        public RemoveGuidFromMemberLogic() : base("Remove GUID From Member")
        {

        }

        public async Task<Embed?> RemoveGuidFromMemberProcess(IUser member,  string guid)
        {
            return null;
        }
    }
}
