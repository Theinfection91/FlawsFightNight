using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Services
{
    public abstract class BaseDataDriven
    {
        public required string Name { get; set; }
        internal DataContext _dataContext;

        public BaseDataDriven(string name, DataContext dataManager)
        {
            Name = name;
            _dataContext = dataManager;
        }
    }
}
