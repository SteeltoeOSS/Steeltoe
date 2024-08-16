// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.Trace;

public sealed class HttpTraceResultV2 : HttpTraceResult
{
    [JsonPropertyName("traces")]
    public IList<HttpTrace> Traces { get; }

    public HttpTraceResultV2(IList<HttpTrace> traces)
    {
        ArgumentNullException.ThrowIfNull(traces);
        ArgumentGuard.ElementsNotNull(traces);

        Traces = traces;
        CurrentVersion = MediaTypeVersion.V2;
    }
}
