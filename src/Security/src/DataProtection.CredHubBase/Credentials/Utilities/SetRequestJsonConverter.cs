// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Steeltoe.Security.DataProtection.CredHub.Credentials.Utilities;

[System.Obsolete("This feature will be removed in the next major version. See https://steeltoe.io/docs/v3/obsolete for details.")]
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
            if (prop.GetValue(value.Value) is object || !options.IgnoreNullValues)
            {
                writer.WriteString(prop.Name.ToLower(), prop.GetValue(value.Value)?.ToString());
            }
#pragma warning restore SYSLIB0020 // Type or member is obsolete
        }

        writer.WriteEndObject();
        writer.WriteString("name", value.Name);
        writer.WriteString("type", value.Type.ToString());
        writer.WriteEndObject();
    }
}