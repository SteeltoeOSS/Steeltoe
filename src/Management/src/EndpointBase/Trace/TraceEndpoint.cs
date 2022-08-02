// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;

namespace Steeltoe.Management.Endpoint.Trace;

public class TraceEndpoint : AbstractEndpoint<List<TraceResult>>, ITraceEndpoint
{
    private readonly ILogger<TraceEndpoint> _logger;
    private readonly ITraceRepository _traceRepo;

    public new ITraceOptions Options => options as ITraceOptions;

    public TraceEndpoint(ITraceOptions options, ITraceRepository traceRepository, ILogger<TraceEndpoint> logger = null)
        : base(options)
    {
        _traceRepo = traceRepository ?? throw new ArgumentNullException(nameof(traceRepository));
        _logger = logger;
    }

    public override List<TraceResult> Invoke()
    {
        return DoInvoke(_traceRepo);
    }

    public List<TraceResult> DoInvoke(ITraceRepository repo)
    {
        return repo.GetTraces();
    }
}
