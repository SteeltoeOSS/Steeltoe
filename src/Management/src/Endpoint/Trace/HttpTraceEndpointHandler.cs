// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.Trace;

internal sealed class HttpTraceEndpointHandler : IHttpTraceEndpointHandler
{
    private readonly IOptionsMonitor<TraceEndpointOptions> _options;
    private readonly IHttpTraceRepository _traceRepo;
    private readonly ILogger<HttpTraceEndpointHandler> _logger;

    public HttpMiddlewareOptions Options => _options.CurrentValue;

    public HttpTraceEndpointHandler(IOptionsMonitor<TraceEndpointOptions> options, IHttpTraceRepository traceRepository, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(options);
        ArgumentGuard.NotNull(traceRepository);
        ArgumentGuard.NotNull(loggerFactory);

        _options = options;
        _traceRepo = traceRepository;
        _logger = loggerFactory.CreateLogger<HttpTraceEndpointHandler>();
    }

    private HttpTraceResult DoInvoke(IHttpTraceRepository repo)
    {
        return repo.GetTraces();
    }

    public Task<HttpTraceResult> InvokeAsync(object argument, CancellationToken cancellationToken)
    {
        _logger.LogTrace("Fetching Traces");
        return Task.Run(() => DoInvoke(_traceRepo), cancellationToken);
    }
}
