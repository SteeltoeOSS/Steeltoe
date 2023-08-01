// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Common.Net;

namespace Steeltoe.Management.Wavefront.Exporters;

public sealed class WavefrontExporterOptions
{
    // Note: this key is shared between tracing and metrics to mirror the Spring boot configuration settings.
    private const string WavefrontPrefix = "management:metrics:export:wavefront";

    private readonly WavefrontApplicationOptions? _applicationOptions;
    private string? _source;

    public string? Uri { get; set; }
    public string? ApiToken { get; set; }
    public int Step { get; set; } = 30_000; // milliseconds
    public int BatchSize { get; set; } = 10_000;
    public int MaxQueueSize { get; set; } = 500_000;
    public string Source => _source ??= _applicationOptions?.Source ?? DnsTools.ResolveHostName();
    public string Name => _applicationOptions?.Name ?? "SteeltoeApp";
    public string Service => _applicationOptions?.Service ?? "SteeltoeAppService";

    public WavefrontExporterOptions(IConfiguration configuration)
    {
        ArgumentGuard.NotNull(configuration);

        configuration.GetSection(WavefrontPrefix).Bind(this);
        _applicationOptions = new WavefrontApplicationOptions(configuration);
    }
}
