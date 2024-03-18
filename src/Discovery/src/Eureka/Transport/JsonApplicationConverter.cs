// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Steeltoe.Discovery.Eureka.Transport;

internal sealed class JsonApplicationConverter : JsonConverter<IList<JsonApplication?>>
{
    public override IList<JsonApplication?> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            return JsonSerializer.Deserialize<List<JsonApplication?>>(ref reader, options)!;
        }

        var application = JsonSerializer.Deserialize<JsonApplication>(ref reader, options);
        return application != null ? [application] : [];
    }

    public override void Write(Utf8JsonWriter writer, IList<JsonApplication?> value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
