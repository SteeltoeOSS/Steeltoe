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

    public IEndpointOptions Options => _options.CurrentValue;

    public HttpTraceEndpoint(IOptionsMonitor<TraceEndpointOptions> options, IHttpTraceRepository traceRepository, ILogger<HttpTraceEndpoint> logger)
    {
        ArgumentGuard.NotNull(traceRepository);
        _options = options;
        _traceRepo = traceRepository;
    }

    public HttpTraceResult Invoke()
    {
        return DoInvoke(_traceRepo);
    }

    public HttpTraceResult DoInvoke(IHttpTraceRepository repo)
    {
        return repo.GetTraces();
    }
}
