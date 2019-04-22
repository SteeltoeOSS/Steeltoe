using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats.Measurements
{
    [Obsolete("Use OpenCensus project packages")]
    public interface IMeasurementDouble : IMeasurement
    {
        double Value { get; }
    }
}
