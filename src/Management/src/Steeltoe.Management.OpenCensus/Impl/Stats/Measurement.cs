using System;
using System.Collections.Generic;
using System.Text;
using Steeltoe.Management.Census.Stats.Measurements;

namespace Steeltoe.Management.Census.Stats
{
    [Obsolete("Use OpenCensus project packages")]
    public abstract class Measurement : IMeasurement
    {
        public abstract IMeasure Measure { get; }

        public abstract M Match<M>(Func<IMeasurementDouble, M> p0, Func<IMeasurementLong, M> p1, Func<IMeasurement, M> defaultFunction);
    }
}
