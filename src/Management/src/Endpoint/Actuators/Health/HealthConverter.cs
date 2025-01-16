// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Steeltoe.Common.CasingConventions;

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

        if (value.Groups.Count > 0)
        {
            writer.WriteStartArray("groups");

            foreach (string group in value.Groups)
            {
                writer.WriteStringValue(group);
            }

            writer.WriteEndArray();
        }

        if (!string.IsNullOrEmpty(value.Description))
        {
            writer.WriteString("description", value.Description);
        }

        if (value.Components.Count > 0)
        {
            writer.WritePropertyName("components");
            writer.WriteStartObject();

            foreach (KeyValuePair<string, object> component in value.Components)
            {
                writer.WritePropertyName(component.Key);
                JsonSerializer.Serialize(writer, component.Value, options);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }
}
