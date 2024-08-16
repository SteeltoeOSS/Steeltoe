// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.Trace;

public sealed class TraceResult
{
    [JsonPropertyName("timestamp")]
    public long TimeStamp { get; }

    [JsonPropertyName("info")]
    public IDictionary<string, object?> Info { get; }

    public TraceResult(long timestamp, IDictionary<string, object?> info)
    {
        ArgumentNullException.ThrowIfNull(info);

        TimeStamp = timestamp;
        Info = info;
    }
}
