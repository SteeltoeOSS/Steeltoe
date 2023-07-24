// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.Metrics;

internal sealed class ConfigureMetricsEndpointOptions : ConfigureEndpointOptions<MetricsEndpointOptions>
{
    private const string ManagementInfoPrefix = "management:endpoints:metrics";

    private const string DefaultIngressIgnorePattern =
        "/cloudfoundryapplication|/cloudfoundryapplication/.*|.*\\.png|.*\\.css|.*\\.js|.*\\.html|/favicon.ico|.*\\.gif";

    private const string DefaultEgressIgnorePattern = "/api/v2/spans|/v2/apps/.*/permissions";

    public ConfigureMetricsEndpointOptions(IConfiguration configuration)
        : base(configuration, ManagementInfoPrefix, "metrics")
    {
    }

    public override void Configure(MetricsEndpointOptions options)
    {
        base.Configure(options);

        if (string.IsNullOrEmpty(options.IngressIgnorePattern))
        {
            options.IngressIgnorePattern = DefaultIngressIgnorePattern;
        }

        if (string.IsNullOrEmpty(options.EgressIgnorePattern))
        {
            options.EgressIgnorePattern = DefaultEgressIgnorePattern;
        }
    }
}
