// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Steeltoe.Security.DataProtection.CredHub.Credentials.Utilities;

public class SetRequestJsonConverter : JsonConverter<CredentialSetRequest>
{
    public override CredentialSetRequest Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, CredentialSetRequest value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteStartObject("value");

        foreach (PropertyInfo prop in value.Value.GetType().GetProperties())
        {
            if (prop.GetValue(value.Value) != null || options.DefaultIgnoreCondition != JsonIgnoreCondition.WhenWritingNull)
            {
#pragma warning disable S4040 // Strings should be normalized to uppercase
                writer.WriteString(prop.Name.ToLowerInvariant(), prop.GetValue(value.Value)?.ToString());
#pragma warning restore S4040 // Strings should be normalized to uppercase
            }
        }

        writer.WriteEndObject();
        writer.WriteString("name", value.Name);
        writer.WriteString("type", value.Type.ToString());
        writer.WriteEndObject();
    }
}
