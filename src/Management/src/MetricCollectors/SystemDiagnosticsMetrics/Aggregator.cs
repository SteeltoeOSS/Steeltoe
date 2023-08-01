#pragma warning disable
// Steeltoe: Copy of version in System.Diagnostics.Metrics (see README.md for details).

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.Tracing;

namespace Steeltoe.Management.MetricCollectors.SystemDiagnosticsMetrics
{
    internal abstract class Aggregator
    {
        // This can be called concurrently with Collect()
        public abstract void Update(double measurement);

        // This can be called concurrently with Update()
        public abstract IAggregationStatistics Collect();
    }

    internal interface IAggregationStatistics { }

    internal readonly struct QuantileValue
    {
        public QuantileValue(double quantile, double value)
        {
            Quantile = quantile;
            Value = value;
        }
        public double Quantile { get; }
        public double Value { get; }
    }

    internal sealed class HistogramStatistics : IAggregationStatistics
    {
        // Steeltoe-Start: Track sum and max.
        //internal HistogramStatistics(QuantileValue[] quantiles)
        internal HistogramStatistics(QuantileValue[] quantiles, double sum, double max)
        // Steeltoe-End: Track sum and max.
        {
            Quantiles = quantiles;

            // Steeltoe-Start: Track sum and max.
            HistogramSum = sum;
            HistogramMax = max;
            // Steeltoe-End: Track sum and max.
        }

        public QuantileValue[] Quantiles { get; }

        // Steeltoe-Start: Track sum and max.
        public double HistogramSum { get; }
        public double HistogramMax { get; }
        // Steeltoe-End: Track sum and max.
    }

    internal sealed class LabeledAggregationStatistics
    {
        public LabeledAggregationStatistics(IAggregationStatistics stats, params KeyValuePair<string, string>[] labels)
        {
            AggregationStatistics = stats;
            Labels = labels;
        }

        public KeyValuePair<string, string>[] Labels { get; }
        public IAggregationStatistics AggregationStatistics { get; }
    }
}
