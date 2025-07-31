using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFights.Managers
{
    public abstract class BaseDataDriven
    {
        public required string Name { get; set; }
        internal DataManager _dataManager;

        public BaseDataDriven(string name, DataManager dataManager)
        {
            Name = name;
            _dataManager = dataManager;
        }
    }
}
