// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Management.Endpoint.Metrics;

public class MetricsEndpointOptions : EndpointOptionsBase//, IMetricsEndpointOptions
{
    internal const string ManagementInfoPrefix = "management:endpoints:metrics";

    internal const string DefaultIngressIgnorePattern =
        "/cloudfoundryapplication|/cloudfoundryapplication/.*|.*\\.png|.*\\.css|.*\\.js|.*\\.html|/favicon.ico|.*\\.gif";

    internal const string DefaultEgressIgnorePattern = "/api/v2/spans|/v2/apps/.*/permissions";

    public string IngressIgnorePattern { get; set; }

    public string EgressIgnorePattern { get; set; }

    public int CacheDurationMilliseconds { get; set; } = 500;
    public int MaxTimeSeries { get; set; } = 100;
    public int MaxHistograms { get; set; } = 100;
    public List<string> IncludedMetrics { get; set; }

    public EndpointOptionsBase EndpointOptions { get; set; }
    public override bool ExactMatch => false;

    public MetricsEndpointOptions()
    {
        Id = "metrics";

        IngressIgnorePattern = DefaultIngressIgnorePattern;
        EgressIgnorePattern = DefaultEgressIgnorePattern;
    }

    //public MetricsEndpointOptions(IConfiguration configuration)
    //    : base(ManagementInfoPrefix, configuration)
    //{
    //    if (string.IsNullOrEmpty(Id))
    //    {
    //        Id = "metrics";
    //    }

    //    if (string.IsNullOrEmpty(IngressIgnorePattern))
    //    {
    //        IngressIgnorePattern = DefaultIngressIgnorePattern;
    //    }

    //    if (string.IsNullOrEmpty(EgressIgnorePattern))
    //    {
    //        EgressIgnorePattern = DefaultEgressIgnorePattern;
    //    }

    //    ExactMatch = false;
    //}
}
