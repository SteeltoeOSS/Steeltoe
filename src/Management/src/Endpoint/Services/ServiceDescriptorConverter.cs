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
     
        foreach (var contextKey in value.Keys)
        {
           
            writer.WritePropertyName(contextKey);
            writer.WriteStartObject();
            writer.WritePropertyName("beans");
            writer.WriteStartObject();
            foreach (var (key, serviceDescriptor) in value[contextKey])
            {
                var name = serviceDescriptor.ServiceType.Name;
             
                writer.WritePropertyName(name);
                writer.WriteStartObject();
                writer.WriteString("scope", serviceDescriptor.Lifetime.ToString());
                writer.WriteString("type", key);
                writer.WriteString("resource", serviceDescriptor.ServiceType.AssemblyQualifiedName);
                writer.WritePropertyName("dependencies");
                
                writer.WriteStartArray();
                if (serviceDescriptor.ImplementationType != null)
                {
                    foreach (var constructor in serviceDescriptor.ImplementationType.GetConstructors())
                    {
                       
                        writer.WriteStringValue(constructor.ToString());
                    }

                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            }

            writer.WriteEndObject();
            writer.WriteEndObject();
        }
        writer.WriteEndObject();

        writer.WriteEndObject();
    }
}
