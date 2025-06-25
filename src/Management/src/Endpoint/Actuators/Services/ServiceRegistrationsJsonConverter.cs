// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.Actuators.Services;

internal sealed class ServiceRegistrationsJsonConverter : JsonConverter<IList<ServiceRegistration>>
{
    public override IList<ServiceRegistration> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }

    public override void Write(Utf8JsonWriter writer, IList<ServiceRegistration> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("contexts");
        writer.WriteStartObject();
        writer.WritePropertyName("application");
        writer.WriteStartObject();
        writer.WritePropertyName("beans");
        writer.WriteStartObject();

        foreach (ServiceRegistration registration in value)
        {
            writer.WritePropertyName(registration.Name);
            JsonSerializer.Serialize(writer, registration, options);
        }

        writer.WriteEndObject();
        writer.WriteEndObject();
        writer.WriteEndObject();
        writer.WriteEndObject();
    }
}
