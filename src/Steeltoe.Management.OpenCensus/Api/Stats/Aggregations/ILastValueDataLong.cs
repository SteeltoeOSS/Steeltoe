using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats.Aggregations
{
    public interface ILastValueDataLong : IAggregationData
    {
        long LastValue { get; }
    }
}
