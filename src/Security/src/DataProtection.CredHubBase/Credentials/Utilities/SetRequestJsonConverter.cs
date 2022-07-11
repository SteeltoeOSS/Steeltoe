// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
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
        foreach (var prop in value.Value.GetType().GetProperties())
        {
#pragma warning disable SYSLIB0020 // Type or member is obsolete
            if (prop.GetValue(value.Value) != null || !options.IgnoreNullValues)
#pragma warning restore SYSLIB0020 // Type or member is obsolete
            {
                writer.WriteString(prop.Name.ToLower(), prop.GetValue(value.Value)?.ToString());
            }
        }

        writer.WriteEndObject();
        writer.WriteString("name", value.Name);
        writer.WriteString("type", value.Type.ToString());
        writer.WriteEndObject();
    }
}
