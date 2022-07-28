// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Management.Endpoint.Metrics;

public class MetricsEndpointOptions : AbstractEndpointOptions, IMetricsEndpointOptions
{
    internal const string ManagementInfoPrefix = "management:endpoints:metrics";
    internal const string DefaultIngressIgnorePattern = "/cloudfoundryapplication|/cloudfoundryapplication/.*|.*\\.png|.*\\.css|.*\\.js|.*\\.html|/favicon.ico|/hystrix.stream|.*\\.gif";
    internal const string DefaultEgressIgnorePattern = "/api/v2/spans|/v2/apps/.*/permissions";

    public MetricsEndpointOptions()
    {
        Id = "metrics";
        IngressIgnorePattern = DefaultIngressIgnorePattern;
        EgressIgnorePattern = DefaultEgressIgnorePattern;
        ExactMatch = false;
    }

    public MetricsEndpointOptions(IConfiguration config)
        : base(ManagementInfoPrefix, config)
    {
        if (string.IsNullOrEmpty(Id))
        {
            Id = "metrics";
        }

        if (string.IsNullOrEmpty(IngressIgnorePattern))
        {
            IngressIgnorePattern = DefaultIngressIgnorePattern;
        }

        if (string.IsNullOrEmpty(EgressIgnorePattern))
        {
            EgressIgnorePattern = DefaultEgressIgnorePattern;
        }

        ExactMatch = false;
    }

    public string IngressIgnorePattern { get; set; }

    public string EgressIgnorePattern { get; set; }

    public int ScrapeResponseCacheDurationMilliseconds { get; set; }
}
