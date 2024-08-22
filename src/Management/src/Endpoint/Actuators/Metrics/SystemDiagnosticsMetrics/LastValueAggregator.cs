#pragma warning disable
// Steeltoe: Copy of version in System.Diagnostics.Metrics (see README.md for details).

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Steeltoe.Management.Endpoint.Actuators.Metrics.SystemDiagnosticsMetrics
{
    internal sealed class LastValue : Aggregator
    {
        private double? _lastValue;

        public override void Update(double value)
        {
            _lastValue = value;
        }

        public override IAggregationStatistics Collect()
        {
            lock (this)
            {
                LastValueStatistics stats = new LastValueStatistics(_lastValue);
                _lastValue = null;
                return stats;
            }
        }
    }

    internal sealed class LastValueStatistics : IAggregationStatistics
    {
        internal LastValueStatistics(double? lastValue)
        {
            LastValue = lastValue;
        }

        public double? LastValue { get; }
    }
}
