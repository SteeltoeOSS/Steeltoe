// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Trace;

namespace Steeltoe.Management.Endpoint.Test.Trace;

internal sealed class TestTraceRepo : IHttpTraceRepository
{
    public bool GetTracesCalled { get; set; }

    public HttpTraceResult GetTraces()
    {
        GetTracesCalled = true;
        return new HttpTracesV1(new List<TraceResult>());
    }
}
