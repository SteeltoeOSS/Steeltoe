// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.ThreadDump;

internal sealed class ThreadDumpV2Converter : JsonConverter<IList<ThreadInfo>>
{
    public override IList<ThreadInfo> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }

    public override void Write(Utf8JsonWriter writer, IList<ThreadInfo> value, JsonSerializerOptions options)
    {
        ArgumentGuard.NotNull(writer);

        writer.WriteStartObject();
        writer.WritePropertyName("threads");
        writer.WriteStartObject();

        foreach (ThreadInfo threadInfo in value)
        {
            JsonSerializer.Serialize(writer, threadInfo, options);
        }

        writer.WriteEndObject();
        writer.WriteEndObject();
    }
}
