// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.Trace;

public class TraceEndpoint :  ITraceEndpoint
{
    private readonly ITraceRepository _traceRepo;

    private readonly IOptionsMonitor<TraceEndpointOptions> _options;

    public IEndpointOptions Options => _options.Get(ConfigureTraceEndpointOptions.TraceEndpointOptionNames.V1.ToString());

    public TraceEndpoint(IOptionsMonitor<TraceEndpointOptions> options, ITraceRepository traceRepository, ILogger<TraceEndpoint> logger = null)
    {
        ArgumentGuard.NotNull(traceRepository);
        _options = options;
        _traceRepo = traceRepository;
    }

    public virtual List<TraceResult> Invoke()
    {
        return DoInvoke(_traceRepo);
    }

    public List<TraceResult> DoInvoke(ITraceRepository repo)
    {
        return repo.GetTraces();
    }
}
