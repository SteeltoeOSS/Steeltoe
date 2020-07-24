// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Steeltoe.Security.DataProtection.CredHub
{
    public class UserCredentialJsonConverter : JsonConverter<UserCredential>
    {
        public override void Write(Utf8JsonWriter writer, UserCredential value, JsonSerializerOptions serializer)
        {
            writer.WriteStringValue(value.ToString());
        }

        public override UserCredential Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize<UserCredential>(ref reader, options);
        }

        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(UserCredential);
        }
    }
}
