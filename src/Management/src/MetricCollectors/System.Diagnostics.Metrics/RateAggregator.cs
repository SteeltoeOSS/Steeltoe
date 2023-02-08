// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics.Metrics;

internal sealed class RateSumAggregator : Aggregator
{
    private double _sum;

    public override void Update(double measurement)
    {
#pragma warning disable S2551 // Shared resources should not be used for locking
        lock (this)
        {
            _sum += measurement;
        }
#pragma warning restore S2551 // Shared resources should not be used for locking
    }

    public override IAggregationStatistics Collect()
    {
#pragma warning disable S2551 // Shared resources should not be used for locking
        lock (this)
        {
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
            var stats = new RateStatistics(_sum);
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
            _sum = 0;
            return stats;
        }
#pragma warning restore S2551 // Shared resources should not be used for locking
    }
}

internal sealed class RateAggregator : Aggregator
{
    private double? _prevValue;
    private double _value;

    public override void Update(double measurement)
    {
#pragma warning disable S2551 // Shared resources should not be used for locking
        lock (this)
        {
            _value = measurement;
        }
#pragma warning restore S2551 // Shared resources should not be used for locking
    }

    public override IAggregationStatistics Collect()
    {
#pragma warning disable S2551 // Shared resources should not be used for locking
        lock (this)
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
#pragma warning restore S2551 // Shared resources should not be used for locking
    }
}

internal sealed class RateStatistics : IAggregationStatistics
{
    public double? Delta { get; }

    public RateStatistics(double? delta)
    {
        Delta = delta;
    }
}
