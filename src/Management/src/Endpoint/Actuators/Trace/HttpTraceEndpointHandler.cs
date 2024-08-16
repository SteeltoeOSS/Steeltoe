// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.Trace;

internal sealed class HttpTraceEndpointHandler : IHttpTraceEndpointHandler
{
    private readonly IOptionsMonitor<TraceEndpointOptions> _optionsMonitor;
    private readonly IHttpTraceRepository _traceRepository;
    private readonly ILogger<HttpTraceEndpointHandler> _logger;

    public MediaTypeVersion Version { get; set; } = MediaTypeVersion.V2;

    public EndpointOptions Options =>
        Version == MediaTypeVersion.V2
            ? _optionsMonitor.CurrentValue
            : _optionsMonitor.Get(ConfigureTraceEndpointOptions.TraceEndpointOptionNames.V1.ToString());

    public HttpTraceEndpointHandler(IOptionsMonitor<TraceEndpointOptions> optionsMonitor, IHttpTraceRepository traceRepository, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(traceRepository);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _optionsMonitor = optionsMonitor;
        _traceRepository = traceRepository;
        _logger = loggerFactory.CreateLogger<HttpTraceEndpointHandler>();
    }

    public Task<HttpTraceResult> InvokeAsync(object? argument, CancellationToken cancellationToken)
    {
        _logger.LogTrace("Fetching Traces");
        HttpTraceResult result = _traceRepository.GetTraces();
        return Task.FromResult(result);
    }
}
