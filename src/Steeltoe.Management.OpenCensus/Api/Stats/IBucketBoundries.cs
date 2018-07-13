using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats
{
    public interface IBucketBoundaries
    {
        IList<double> Boundaries { get; }
    }
}
