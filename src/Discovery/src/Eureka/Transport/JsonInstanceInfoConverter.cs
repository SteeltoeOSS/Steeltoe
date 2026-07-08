// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Steeltoe.Discovery.Eureka.Transport;

internal sealed class JsonInstanceInfoConverter : JsonConverter<IList<JsonInstanceInfo?>>
{
    public override IList<JsonInstanceInfo?> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Individual instances are deserialized one at a time (instead of the entire array at once), so that a single instance with invalid
        // values (for example, an unrecognized "actionType") does not prevent the other instances in the array from being discovered.

        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        List<JsonInstanceInfo?> instances = [];

        if (document.RootElement.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement element in document.RootElement.EnumerateArray())
            {
                JsonInstanceInfo? instanceInfo = TryDeserializeInstance(element);

                if (instanceInfo != null)
                {
                    instances.Add(instanceInfo);
                }
            }
        }
        else
        {
            JsonInstanceInfo? instanceInfo = TryDeserializeInstance(document.RootElement);

            if (instanceInfo != null)
            {
                instances.Add(instanceInfo);
            }
        }

        return instances;
    }

    private static JsonInstanceInfo? TryDeserializeInstance(JsonElement element)
    {
        try
        {
            return JsonSerializer.Deserialize(element.GetRawText(), EurekaJsonSerializerContext.Default.JsonInstanceInfo);
        }
        catch (Exception exception) when (exception is JsonException or FormatException)
        {
            return null;
        }
    }

    public override void Write(Utf8JsonWriter writer, IList<JsonInstanceInfo?> value, JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }
}
