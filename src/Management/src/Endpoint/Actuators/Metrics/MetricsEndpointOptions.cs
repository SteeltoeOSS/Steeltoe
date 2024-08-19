// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.Metrics;

public sealed class MetricsEndpointOptions : EndpointOptions
{
    /// <summary>
    /// Gets or sets the duration in milliseconds that metrics are cached for. Default value: 500.
    /// </summary>
    public int CacheDurationMilliseconds { get; set; } = 500;

    /// <summary>
    /// Gets or sets the maximum number of time series to return. Default value: 100.
    /// </summary>
    public int MaxTimeSeries { get; set; } = 100;

    /// <summary>
    /// Gets or sets the maximum number of histograms to return. Default value: 100.
    /// </summary>
    public int MaxHistograms { get; set; } = 100;

    /// <summary>
    /// Gets the names of metrics to include.
    /// </summary>
    public IList<string> IncludedMetrics { get; } = new List<string>();

    /// <inheritdoc />
    public override bool RequiresExactMatch()
    {
        return false;
    }
}
