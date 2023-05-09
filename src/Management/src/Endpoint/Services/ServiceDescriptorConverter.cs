using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using Steeltoe.Management.Endpoint.Hypermedia;

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
                writer.WriteString("resource", key);
                writer.WriteString("scope", serviceDescriptor.Lifetime.ToString());
                writer.WriteString("type", serviceDescriptor.ServiceType.FullName);
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
