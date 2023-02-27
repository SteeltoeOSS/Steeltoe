// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.Trace;

public class TraceEndpoint : IEndpoint<List<TraceResult>>, ITraceEndpoint
{
    private readonly ITraceRepository _traceRepo;

    public IOptionsMonitor<TraceEndpointOptions> Options { get; }

    IEndpointOptions IEndpoint.Options => Options.CurrentValue;

    // public new ITraceOptions Options => options as ITraceOptions;

    public TraceEndpoint(IOptionsMonitor<TraceEndpointOptions> options, ITraceRepository traceRepository, ILogger<TraceEndpoint> logger = null)
       // : base(options)
    {
        ArgumentGuard.NotNull(traceRepository);
        Options = options;
        _traceRepo = traceRepository;
    }

    public List<TraceResult> Invoke()
    {
        return DoInvoke(_traceRepo);
    }

    public List<TraceResult> DoInvoke(ITraceRepository repo)
    {
        return repo.GetTraces();
    }
}
