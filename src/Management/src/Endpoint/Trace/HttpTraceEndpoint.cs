// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.Trace;

public class HttpTraceEndpoint : IHttpTraceEndpoint
{
    private readonly IOptionsMonitor<TraceEndpointOptions> _options;
    private readonly IHttpTraceRepository _traceRepo;
    private readonly ILogger<HttpTraceEndpoint> _logger;

    public IEndpointOptions Options => _options.CurrentValue;

    public HttpTraceEndpoint(IOptionsMonitor<TraceEndpointOptions> options, IHttpTraceRepository traceRepository, ILogger<HttpTraceEndpoint> logger)
    {
        ArgumentGuard.NotNull(traceRepository);
        ArgumentGuard.NotNull(logger);

        _options = options;
        _traceRepo = traceRepository;
        _logger = logger;
    }

    public HttpTraceResult Invoke()
    {
        _logger.LogTrace("Fetching Traces");
        return DoInvoke(_traceRepo);
    }

    private HttpTraceResult DoInvoke(IHttpTraceRepository repo)
    {
        return repo.GetTraces();
    }
}
