// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.Trace;

public sealed class HttpTracesV1 : HttpTraceResult
{
    public IList<TraceResult> Traces { get; }

    public HttpTracesV1(IList<TraceResult> traces)
    {
        Traces = traces;
        CurrentVersion = MediaTypeVersion.V1;
    }
}
