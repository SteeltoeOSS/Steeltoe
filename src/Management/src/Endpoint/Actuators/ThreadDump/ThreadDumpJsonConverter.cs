// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.Actuators.ThreadDump;

internal sealed class ThreadDumpJsonConverter : JsonConverter<IList<ThreadInfo>>
{
    public override IList<ThreadInfo> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }

    public override void Write(Utf8JsonWriter writer, IList<ThreadInfo> value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteStartObject();
        writer.WritePropertyName("threads");
        writer.WriteStartArray();

        foreach (ThreadInfo threadInfo in value)
        {
            JsonSerializer.Serialize(writer, threadInfo, options);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}
