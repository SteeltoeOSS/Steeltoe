// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using Steeltoe.Common.HealthChecks;
using System;

namespace Steeltoe.Management.Endpoint.Health
{
    [Obsolete] // delete
    public class HealthJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(HealthCheckResult);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            if (value is HealthCheckResult health)
            {
                writer.WritePropertyName("status");
                writer.WriteValue(health.Status.ToString());
                if (!string.IsNullOrEmpty(health.Description))
                {
                    writer.WritePropertyName("description");
                    writer.WriteValue(health.Description);
                }

                foreach (var detail in health.Details)
                {
                    writer.WritePropertyName(detail.Key);
                    serializer.Serialize(writer, detail.Value);
                }
            }

            writer.WriteEndObject();
        }
    }
}
