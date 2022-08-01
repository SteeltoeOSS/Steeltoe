// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Steeltoe.Discovery.Eureka.Transport;

internal sealed class JsonApplicationConverter : JsonConverter<List<JsonApplication>>
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert == typeof(IList<JsonApplication>);
    }

    public override List<JsonApplication> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        List<JsonApplication> result = null;
        try
        {
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                result = JsonSerializer.Deserialize<List<JsonApplication>>(ref reader, options);
            }
            else
            {
                var singleInst = JsonSerializer.Deserialize<JsonApplication>(ref reader, options);
                if (singleInst != null)
                {
                    result = new List<JsonApplication>
                    {
                        singleInst
                    };
                }
            }
        }
        catch (Exception)
        {
            result = new List<JsonApplication>();
        }

        result ??= new List<JsonApplication>();

        return result;
    }

    public override void Write(Utf8JsonWriter writer, List<JsonApplication> value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
