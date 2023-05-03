// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Trace;

namespace Steeltoe.Management.Endpoint.Test.Trace;

internal sealed class TestTraceEndpoint : TraceEndpoint
{
    public TestTraceEndpoint(IOptionsMonitor<TraceEndpointOptions> options, ITraceRepository traceRepository, ILogger<TraceEndpoint> logger)
        : base(options, traceRepository, logger)
    {
    }

    public override List<TraceResult> Invoke()
    {
        return new List<TraceResult>();
    }
}
