// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Configuration;

#pragma warning disable S4004 // Collection properties should be readonly

namespace Steeltoe.Management.Endpoint.Actuators.Metrics;

public sealed class MetricsEndpointOptions : EndpointOptions
{
    public int CacheDurationMilliseconds { get; set; } = 500;
    public int MaxTimeSeries { get; set; } = 100;
    public int MaxHistograms { get; set; } = 100;
    public IList<string> IncludedMetrics { get; set; } = new List<string>();

    public override bool RequiresExactMatch()
    {
        return false;
    }
}
