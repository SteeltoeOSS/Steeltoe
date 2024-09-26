// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Diagnostics;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.Metrics;

internal sealed class ConfigureMetricsObserverOptions : IConfigureOptionsWithKey<MetricsObserverOptions>
{
    private const string ManagementMetricsPrefix = "management:metrics:observer";

    internal const string DefaultIngressIgnorePattern =
        @"/cloudfoundryapplication|/cloudfoundryapplication/.*|.*\.png|.*\.css|.*\.js|.*\.html|/favicon.ico|.*\.gif";

    internal const string DefaultEgressIgnorePattern = "/api/v2/spans|/v2/apps/.*/permissions";
    private readonly IConfiguration _configuration;

    public string ConfigurationKey => ManagementMetricsPrefix;

    public ConfigureMetricsObserverOptions(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _configuration = configuration;
    }

    public void Configure(MetricsObserverOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

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
