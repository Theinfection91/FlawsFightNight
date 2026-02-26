using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Models.UT2004
{
    public class UT2004OpenSkillRating
    {
        public double Mu { get; set; } = 25.0;
        public double Sigma { get; set; } = 25.0 / 3.0;
        public double Rating => Mu - (3 * Sigma);

        public UT2004OpenSkillRating() { }

        public void UpdateSkillRating(double newMu, double newSigma)
        {
            Mu = newMu;
            Sigma = newSigma;
        }
    }
}
