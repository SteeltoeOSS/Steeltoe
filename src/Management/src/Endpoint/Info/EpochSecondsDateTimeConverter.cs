// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.Info;

// ReSharper disable once UnusedType.Global
public sealed class EpochSecondsDateTimeConverter : JsonConverter<DateTime>
{
    private static readonly DateTime BaseTime = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? value = reader.GetString();

        if (value == null)
        {
            return BaseTime;
        }

        return DateTime.Parse(value, CultureInfo.InvariantCulture);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        ArgumentGuard.NotNull(writer);

        DateTime utc = value.ToUniversalTime();
        long valueToInsert = (utc.Ticks - BaseTime.Ticks) / 10000;
        writer.WriteNumberValue(valueToInsert);
    }
}
