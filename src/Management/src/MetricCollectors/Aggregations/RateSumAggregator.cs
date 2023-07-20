// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.MetricCollectors.Aggregations;

internal sealed class RateSumAggregator : Aggregator
{
    private readonly object _lockObject = new();
    private double _sum;

    public override void Update(double measurement)
    {
        lock (_lockObject)
        {
            _sum += measurement;
        }
    }

    public override IAggregationStatistics Collect()
    {
        lock (_lockObject)
        {
            var stats = new RateStatistics(_sum);
            _sum = 0;
            return stats;
        }
    }
}
