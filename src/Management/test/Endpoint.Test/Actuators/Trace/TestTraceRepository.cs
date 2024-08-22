// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Actuators.Trace;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Trace;

internal sealed class TestTraceRepository : IHttpTraceRepository
{
    public bool GetTracesCalled { get; private set; }

    public HttpTraceResult GetTraces()
    {
        GetTracesCalled = true;
        return new HttpTraceResultV1(new List<TraceResult>());
    }
}
