using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Interfaces
{
    public interface ITieBreaker
    {
        ITieBreakerRule TieBreakerRule { get; set; }
        void ApplyTieBreaker();
    }
}
