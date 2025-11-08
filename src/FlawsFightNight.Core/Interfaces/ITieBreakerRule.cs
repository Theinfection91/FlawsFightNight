using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Interfaces
{
    public interface ITieBreakerRule
    {
        (string, string) ResolveTie(List<string> tiedTeams, IMatchLog matchLog);
        string Name { get; }
        }
}
