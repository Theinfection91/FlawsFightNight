using FlawsFightNight.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Interfaces
{
    public interface ITieBreakerRule
    {
        (string, string) ResolveTie(List<string> tiedTeams, MatchLog matchLog);
        string Name { get; }
        }
}
