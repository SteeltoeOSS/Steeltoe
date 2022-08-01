// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Steeltoe.Security.DataProtection.CredHub;

public class StringCredentialJsonConverter<T> : JsonConverter<T>
{
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return (T)Activator.CreateInstance(typeToConvert, JsonSerializer.Deserialize<string>(ref reader, options));
    }

    public override bool CanConvert(Type typeToConvert)
    {
        if (typeToConvert.IsAssignableFrom(typeof(T)) && !typeToConvert.IsInterface)
        {
            return true;
        }

        return false;
    }
}
