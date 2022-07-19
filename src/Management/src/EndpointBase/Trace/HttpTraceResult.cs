// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.Trace;

public class HttpTraceResult
{
    private readonly List<TraceResult> _list;

    [JsonPropertyName("traces")]
    public List<HttpTrace> Traces { get; }

    public HttpTraceResult(List<HttpTrace> traces)
    {
        Traces = traces;
    }

    public HttpTraceResult(List<TraceResult> list)
    {
        _list = list;
    }
}