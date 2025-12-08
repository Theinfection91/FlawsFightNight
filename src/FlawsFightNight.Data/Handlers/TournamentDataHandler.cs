using FlawsFightNight.Data.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Data.Handlers
{
    public class TournamentDataHandler : BaseFolderDataHandler<TournamentDataFile>
    {
        public TournamentDataHandler(string tournamentId) : base(tournamentId)
        {

        }
    }
}
