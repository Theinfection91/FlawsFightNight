using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic
{
    public abstract class Logic
    {
        public required string Name { get; set; }

        protected Logic(string name)
        {
            Name = name;
        }
    }
}
