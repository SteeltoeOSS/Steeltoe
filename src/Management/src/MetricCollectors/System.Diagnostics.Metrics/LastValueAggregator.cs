// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics.Metrics;

internal sealed class LastValue : Aggregator
{
    private double? _lastValue;

    public override void Update(double measurement)
    {
        _lastValue = measurement;
    }

    public override IAggregationStatistics Collect()
    {
#pragma warning disable S2551 // Shared resources should not be used for locking
        lock (this)
        {
            var stats = new LastValueStatistics(_lastValue);
            _lastValue = null;
            return stats;
        }
#pragma warning restore S2551 // Shared resources should not be used for locking
    }
}

internal sealed class LastValueStatistics : IAggregationStatistics
{
    public double? LastValue { get; }

    internal LastValueStatistics(double? lastValue)
    {
        LastValue = lastValue;
    }
}
