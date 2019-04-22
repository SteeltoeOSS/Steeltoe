using Steeltoe.Management.Census.Stats.Measures;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats.Measurements
{
    public interface IMeasurementLong : IMeasurement
    {
        long Value { get; }
    }
}
