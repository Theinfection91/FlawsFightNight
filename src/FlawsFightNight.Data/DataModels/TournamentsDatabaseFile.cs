using FlawsFightNight.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Data.DataModels
{
    public class TournamentsDatabaseFile
    {
        public List<Tournament> Tournaments { get; set; } = new List<Tournament>();

        public TournamentsDatabaseFile() { }
    }
}
