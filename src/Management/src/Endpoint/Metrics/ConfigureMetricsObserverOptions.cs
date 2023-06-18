// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Diagnostics;

namespace Steeltoe.Management.Endpoint.Metrics;

internal class ConfigureMetricsObserverOptions : IConfigureOptions<MetricsObserverOptions>
{
    internal const string ManagementMetricsPrefix = "management:metrics:observer";

    internal const string DefaultIngressIgnorePattern =
        "/cloudfoundryapplication|/cloudfoundryapplication/.*|.*\\.png|.*\\.css|.*\\.js|.*\\.html|/favicon.ico|.*\\.gif";

    internal const string DefaultEgressIgnorePattern = "/api/v2/spans|/v2/apps/.*/permissions";
    private readonly IConfiguration _configuration;

    public ConfigureMetricsObserverOptions(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void Configure(MetricsObserverOptions options)
    {
        _configuration.GetSection(ManagementMetricsPrefix).Bind(options);

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
