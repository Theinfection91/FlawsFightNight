using FlawsFightNight.Core.Models.UT2004;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Interfaces.UT2004
{
    public interface IUTGameModeWeight
    {
        /// <summary>
        /// Returns additive objective weight value to be added to the adjusted score.
        /// 'baseScore' is the current adjusted score context. 'opponent' may be null for avg-team path.
        /// </summary>
        double ApplyObjectiveWeights(UTPlayerMatchStats player, double baseScore, UTPlayerMatchStats? opponent = null);
    }
}
