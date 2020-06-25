// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.HealthChecks;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.Health
{
    public class HealthConverter : JsonConverter<HealthCheckResult>
    {
        public override HealthCheckResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, HealthCheckResult value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            if (value is HealthCheckResult health)
            {
                writer.WriteString("status", health.Status.ToString());
                if (!string.IsNullOrEmpty(health.Description))
                {
                    writer.WriteString("description", health.Description);
                }

                foreach (var detail in health.Details)
                {
                    writer.WritePropertyName(detail.Key);
                    JsonSerializer.Serialize(writer, detail.Value, options);
                }
            }

            writer.WriteEndObject();
        }
    }
}