// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Net;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Wavefront.Exporters;

internal sealed class ConfigureWavefrontExporterOptions : IConfigureOptionsWithKey<WavefrontExporterOptions>
{
    private const string WavefrontApplicationPrefix = "wavefront:application";

    // Note: this key is shared between tracing and metrics to mirror the Spring boot configuration settings.
    private const string WavefrontMetricsPrefix = "management:metrics:export:wavefront";

    private readonly IConfiguration _configuration;

    public ConfigureWavefrontExporterOptions(IConfiguration configuration)
    {
        ArgumentGuard.NotNull(configuration);

        _configuration = configuration;
    }

    public string ConfigurationKey => WavefrontMetricsPrefix;

    public void Configure(WavefrontExporterOptions options)
    {
        ArgumentGuard.NotNull(options);

        _configuration.GetSection(WavefrontMetricsPrefix).Bind(options);

        var applicationOptions = new WavefrontApplicationOptions();
        _configuration.GetSection(WavefrontApplicationPrefix).Bind(options);

        options.Source = applicationOptions.Source ?? DnsTools.ResolveHostName();
        options.Name = applicationOptions.Name ?? "SteeltoeApp";
        options.Service = applicationOptions.Service ?? "SteeltoeAppService";
    }
}
