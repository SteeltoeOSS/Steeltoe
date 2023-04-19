// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.Trace;

internal class TraceEndpoint : ITraceEndpoint
{
    private readonly ITraceRepository _traceRepo;
    private readonly ILogger<TraceEndpoint> _logger;
    private readonly IOptionsMonitor<TraceEndpointOptions> _options;

    public IEndpointOptions Options => _options.Get(ConfigureTraceEndpointOptions.TraceEndpointOptionNames.V1.ToString());

    public TraceEndpoint(IOptionsMonitor<TraceEndpointOptions> options, ITraceRepository traceRepository, ILogger<TraceEndpoint> logger)
    {
        ArgumentGuard.NotNull(traceRepository);
        ArgumentGuard.NotNull(logger);
        _options = options;
        _traceRepo = traceRepository;
        _logger = logger;
    }

    public virtual IList<TraceResult> Invoke()
    {
        _logger.LogTrace("Fetching Traces");
        return DoInvoke(_traceRepo);
    }

    private IList<TraceResult> DoInvoke(ITraceRepository repo)
    {
        return repo.GetTraces();
    }
}
