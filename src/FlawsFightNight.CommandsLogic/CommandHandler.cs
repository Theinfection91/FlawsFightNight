using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands
{
    public abstract class CommandHandler
    {
        public required string Name { get; set; }

        protected CommandHandler(string name)
        {
            Name = name;
        }
    }
}
