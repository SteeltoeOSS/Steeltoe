// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.Metrics;

public class MetricsEndpointOptions : EndpointOptionsBase
{
    public string IngressIgnorePattern { get; set; }

    public string EgressIgnorePattern { get; set; }

    public int CacheDurationMilliseconds { get; set; } = 500;
    public int MaxTimeSeries { get; set; } = 100;
    public int MaxHistograms { get; set; } = 100;
    public List<string> IncludedMetrics { get; set; }

    public EndpointOptionsBase EndpointOptions { get; set; }
    public override bool ExactMatch => false;
}
