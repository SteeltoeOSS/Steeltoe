// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics.Metrics;

internal abstract class Aggregator
{
    // This can be called concurrently with Collect()
    public abstract void Update(double measurement);

    // This can be called concurrently with Update()
    public abstract IAggregationStatistics Collect();
}

[SuppressMessage("Minor Code Smell", "S4023:Interfaces should not be empty", Justification = "Cannot be replaced by attributes as suggested")]
public interface IAggregationStatistics
{
}

#pragma warning disable S3898 // Value types should implement "IEquatable<T>"
internal readonly struct QuantileValue
#pragma warning restore S3898 // Value types should implement "IEquatable<T>"
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
    public QuantileValue[] Quantiles { get; }

    public double HistogramSum { get; }
    public double HistograMax { get; }

    internal HistogramStatistics(QuantileValue[] quantiles, double sum, double max)
    {
        Quantiles = quantiles;
        HistogramSum = sum;
        HistograMax = max;
    }
}

public sealed class LabeledAggregationStatistics
{
    public KeyValuePair<string, string>[] Labels { get; }
    public IAggregationStatistics AggregationStatistics { get; }

    public LabeledAggregationStatistics(IAggregationStatistics stats, params KeyValuePair<string, string>[] labels)
    {
        AggregationStatistics = stats;
        Labels = labels;
    }
}
