using Steeltoe.Management.Census.Stats.Measures;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats
{
    [Obsolete("Use OpenCensus project packages")]
    public interface IMeasure
    {
        M Match<M>(
            Func<IMeasureDouble, M> p0,
            Func<IMeasureLong, M> p1,
            Func<IMeasure, M> defaultFunction);
        string Name { get; }
        string Description { get; }
        string Unit { get; }

    }
}
