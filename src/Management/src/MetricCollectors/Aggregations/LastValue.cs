// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.MetricCollectors.Aggregations;

internal sealed class LastValue : Aggregator
{
    private readonly object _lockObject = new();
    private double? _lastValue;

    public override void Update(double measurement)
    {
        _lastValue = measurement;
    }

    public override IAggregationStatistics Collect()
    {
        lock (_lockObject)
        {
            var stats = new LastValueStatistics(_lastValue);
            _lastValue = null;
            return stats;
        }
    }
}
