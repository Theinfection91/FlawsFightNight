using FlawsFightNight.IO.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.IO.Handlers
{
    public class PermissionsConfigHandler : AsyncDataHandler<PermissionsConfigFile>
    {
        public PermissionsConfigHandler() : base(PathOption.Databases, "permissions_config.json")
        {

        }
    }
}
