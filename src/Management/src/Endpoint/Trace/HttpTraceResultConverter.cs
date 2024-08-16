// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.Trace;

internal sealed class HttpTraceResultConverter : JsonConverter<HttpTraceResult>
{
    public override HttpTraceResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }

    public override void Write(Utf8JsonWriter writer, HttpTraceResult value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);

        if (value is HttpTraceResultV2 traceResultV2)
        {
            JsonSerializer.Serialize(writer, traceResultV2, options);
        }
        else if (value is HttpTraceResultV1 traceResultV1)
        {
            JsonSerializer.Serialize(writer, traceResultV1, options);
        }
    }
}
