// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Steeltoe.Security.DataProtection.CredHub
{
    public class JsonCredentialJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteRawValue(value.ToString());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return new JsonCredential(JObject.Load(reader));
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(JsonCredential);
        }
    }
}
