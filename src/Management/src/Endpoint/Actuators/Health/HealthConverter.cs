// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Steeltoe.Common.CasingConventions;
using Steeltoe.Common.HealthChecks;

namespace Steeltoe.Management.Endpoint.Actuators.Health;

internal sealed class HealthConverter : JsonConverter<HealthEndpointResponse>
{
    public override HealthEndpointResponse Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }

    public override void Write(Utf8JsonWriter writer, HealthEndpointResponse value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteStartObject();
        writer.WriteString("status", value.Status.ToSnakeCaseString(SnakeCaseStyle.AllCaps));

        if (!string.IsNullOrEmpty(value.Description))
        {
            writer.WriteString("description", value.Description);
        }

        if (value.Details.Count > 0)
        {
            writer.WritePropertyName("details");
            writer.WriteStartObject();

            foreach ((string detailKey, object detailValue) in value.Details)
            {
                writer.WritePropertyName(detailKey);

                if (detailValue is HealthCheckResult result)
                {
                    var details = new Dictionary<string, object>
                    {
                        ["status"] = result.Status.ToSnakeCaseString(SnakeCaseStyle.AllCaps)
                    };

                    if (result.Description != null)
                    {
                        details["description"] = result.Description;
                    }

                    foreach ((string resultKey, object resultValue) in result.Details)
                    {
                        details[resultKey] = resultValue;
                    }

                    JsonSerializer.Serialize(writer, details, options);
                }
                else
                {
                    JsonSerializer.Serialize(writer, detailValue, options);
                }
            }

            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }
}
