// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.Trace;

internal sealed class TraceEndpointHandler : ITraceEndpointHandler
{
    private readonly ITraceRepository _traceRepo;
    private readonly ILogger<TraceEndpointHandler> _logger;
    private readonly IOptionsMonitor<TraceEndpointOptions> _options;

    public HttpMiddlewareOptions Options => _options.Get(ConfigureTraceEndpointOptions.TraceEndpointOptionNames.V1.ToString());

    public TraceEndpointHandler(IOptionsMonitor<TraceEndpointOptions> options, ITraceRepository traceRepository, ILogger<TraceEndpointHandler> logger)
    {
        ArgumentGuard.NotNull(traceRepository);
        ArgumentGuard.NotNull(logger);
        _options = options;
        _traceRepo = traceRepository;
        _logger = logger;
    }

    public Task<IList<TraceResult>> InvokeAsync(object arg, CancellationToken cancellationToken)
    {
        _logger.LogTrace("Fetching Traces");
        return Task.Run(() => _traceRepo.GetTraces(), cancellationToken);
    }
}
