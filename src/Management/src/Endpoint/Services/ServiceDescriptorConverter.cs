// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.Services;

internal class ServiceDescriptorConverter : JsonConverter<ServiceContextDescriptor>
{
    public override ServiceContextDescriptor Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, ServiceContextDescriptor value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("contexts");
        writer.WriteStartObject();

        foreach (string contextKey in value.Keys)
        {
            writer.WritePropertyName(contextKey);
            writer.WriteStartObject();
            writer.WritePropertyName("beans");
            writer.WriteStartObject();

            foreach (Service service in value[contextKey])
            {
                writer.WritePropertyName(service.Name);
                JsonSerializer.Serialize(writer, service, options);
            }

            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
        writer.WriteEndObject();
    }
}
