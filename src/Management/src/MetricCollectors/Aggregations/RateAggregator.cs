// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.MetricCollectors.Aggregations;

internal sealed class RateAggregator : Aggregator
{
    private readonly object _lockObject = new();
    private double? _prevValue;
    private double _value;

    public override void Update(double measurement)
    {
        lock (_lockObject)
        {
            _value = measurement;
        }
    }

    public override IAggregationStatistics Collect()
    {
        lock (_lockObject)
        {
            double? delta = null;

            if (_prevValue.HasValue)
            {
                delta = _value - _prevValue.Value;
            }

            var stats = new RateStatistics(delta);
            _prevValue = _value;
            return stats;
        }
    }
}
