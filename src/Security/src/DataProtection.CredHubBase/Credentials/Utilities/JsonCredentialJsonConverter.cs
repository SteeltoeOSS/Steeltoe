// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Steeltoe.Security.DataProtection.CredHub;

public class JsonCredentialJsonConverter : JsonConverter<JsonCredential>
{
    public override void Write(Utf8JsonWriter writer, JsonCredential value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }

    public override JsonCredential Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var json = JsonSerializer.Deserialize<JsonElement>(ref reader, options);
        return new JsonCredential(json);
    }

    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert == typeof(JsonCredential);
    }
}
