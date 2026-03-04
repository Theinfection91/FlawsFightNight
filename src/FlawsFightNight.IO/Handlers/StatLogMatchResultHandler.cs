using FlawsFightNight.IO.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.IO.Handlers
{
    public class StatLogMatchResultHandler : AsyncDataHandler<StatLogMatchResultsFile>
    {
        public StatLogMatchResultHandler() : base()
        {

        }
    }
}
