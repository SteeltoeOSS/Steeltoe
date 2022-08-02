// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Util;

namespace Steeltoe.Management.Endpoint.Health;

public class HealthConverter : JsonConverter<HealthEndpointResponse>
{
    public override HealthEndpointResponse Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, HealthEndpointResponse value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        if (value is HealthEndpointResponse health)
        {
            writer.WriteString("status", health.Status.ToSnakeCaseString(SnakeCaseStyle.AllCaps));

            if (!string.IsNullOrEmpty(health.Description))
            {
                writer.WriteString("description", health.Description);
            }

            if (health.Details != null && health.Details.Count > 0)
            {
                writer.WritePropertyName("details");
                writer.WriteStartObject();

                foreach (KeyValuePair<string, object> detail in health.Details)
                {
                    writer.WritePropertyName(detail.Key);

                    if (detail.Value is HealthCheckResult detailValue)
                    {
                        JsonSerializer.Serialize(writer, detailValue.Details, options);
                    }
                    else
                    {
                        JsonSerializer.Serialize(writer, detail.Value, options);
                    }
                }

                writer.WriteEndObject();
            }
        }

        writer.WriteEndObject();
    }
}
