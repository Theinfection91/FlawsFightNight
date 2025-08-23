using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Data.DataModels
{
    public class PermissionsConfigFile
    {
        public List<ulong> DebugAdminList { get; set; } = [];
        public PermissionsConfigFile() { }
    }
}
