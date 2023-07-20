// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.MetricCollectors.Aggregations;

internal sealed class HistogramStatistics : IAggregationStatistics
{
    public QuantileValue[] Quantiles { get; }

    public double HistogramSum { get; }
    public double HistogramMax { get; }

    internal HistogramStatistics(QuantileValue[] quantiles, double sum, double max)
    {
        Quantiles = quantiles;
        HistogramSum = sum;
        HistogramMax = max;
    }
}
