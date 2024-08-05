// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.Info;

// ReSharper disable once UnusedType.Global
public sealed class EpochSecondsDateTimeConverter : JsonConverter<DateTime>
{
    private static readonly DateTime BaseTime = DateTime.UnixEpoch;

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
        ArgumentNullException.ThrowIfNull(writer);

        DateTime utc = value.ToUniversalTime();
        long valueToInsert = (utc.Ticks - BaseTime.Ticks) / 10000;
        writer.WriteNumberValue(valueToInsert);
    }
}
