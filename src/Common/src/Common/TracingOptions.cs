// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Common;

public class TracingOptions : ITracingOptions
{
    internal const string ConfigurationPrefix = "management:tracing";

    internal const string DefaultIngressIgnorePattern = "/actuator/.*|/cloudfoundryapplication/.*|.*\\.png|.*\\.css|.*\\.js|.*\\.html|/favicon.ico|.*\\.gif";

    internal const string DefaultEgressIgnorePattern = "/api/v2/spans|/v2/apps/.*/permissions|/eureka/*";
    internal const int DefaultMaxPayloadSizeInBytes = 4096;
    private readonly IApplicationInstanceInfo _applicationInstanceInfo;

    /// <inheritdoc />
    public string Name => _applicationInstanceInfo?.GetApplicationNameInContext(SteeltoeComponent.Management, $"{ConfigurationPrefix}:name");

    /// <inheritdoc />
    public string IngressIgnorePattern { get; set; }

    /// <inheritdoc />
    public string EgressIgnorePattern { get; set; }

    /// <inheritdoc />
    public int MaxPayloadSizeInBytes { get; set; } = DefaultMaxPayloadSizeInBytes;

    /// <inheritdoc />
    public bool AlwaysSample { get; set; }

    /// <inheritdoc />
    public bool NeverSample { get; set; }

    /// <inheritdoc />
    public bool UseShortTraceIds { get; set; }

    /// <inheritdoc />
    public string PropagationType { get; set; } = "B3";

    /// <inheritdoc />
    public bool SingleB3Header { get; set; } = true;

    /// <inheritdoc />
    public Uri ExporterEndpoint { get; set; }

    public TracingOptions(IApplicationInstanceInfo appInfo, IConfiguration configuration)
    {
        ArgumentGuard.NotNull(configuration);

        IConfigurationSection section = configuration.GetSection(ConfigurationPrefix);

        if (section != null)
        {
            section.Bind(this);
        }

        _applicationInstanceInfo = appInfo;

        if (string.IsNullOrEmpty(IngressIgnorePattern))
        {
            IngressIgnorePattern = DefaultIngressIgnorePattern;
        }

        if (string.IsNullOrEmpty(EgressIgnorePattern))
        {
            EgressIgnorePattern = DefaultEgressIgnorePattern;
        }
    }

    internal TracingOptions()
    {
    }
}
