using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.IO.Interfaces
{
    public interface IAsyncInitializable
    {
        Task InitializePendingPathAsync();
    }
}
