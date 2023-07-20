// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.MetricCollectors.Aggregations;

internal sealed class QuantileAggregation
{
    public double[] Quantiles { get; }
    public double MaxRelativeError => 0.001;

    public QuantileAggregation(params double[] quantiles)
    {
        Quantiles = quantiles;
        Array.Sort(Quantiles);
    }
}
