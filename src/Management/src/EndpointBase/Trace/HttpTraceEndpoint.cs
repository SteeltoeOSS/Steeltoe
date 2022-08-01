// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;

namespace Steeltoe.Management.Endpoint.Trace;

public class HttpTraceEndpoint : AbstractEndpoint<HttpTraceResult>, IHttpTraceEndpoint
{
    private readonly ILogger<HttpTraceEndpoint> _logger;
    private readonly IHttpTraceRepository _traceRepo;

    public HttpTraceEndpoint(ITraceOptions options, IHttpTraceRepository traceRepository, ILogger<HttpTraceEndpoint> logger = null)
        : base(options)
    {
        _traceRepo = traceRepository ?? throw new ArgumentNullException(nameof(traceRepository));
        _logger = logger;
    }

    public new ITraceOptions Options
    {
        get
        {
            return options as ITraceOptions;
        }
    }

    public override HttpTraceResult Invoke()
    {
        return DoInvoke(_traceRepo);
    }

    public HttpTraceResult DoInvoke(IHttpTraceRepository repo)
    {
        return repo.GetTraces();
    }
}
