﻿using FlawsFightNight.Data.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Data.Handlers
{
    public class PermissionsConfigHandler : BaseDataHandler<PermissionsConfigFile>
    {
        public PermissionsConfigHandler() : base("permissions_config.json", "Databases")
        {

        }
    }
}
