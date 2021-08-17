// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.Info
{
    public class EpochSecondsDateTimeConverter : JsonConverter<DateTime>
    {
        private static readonly DateTime _baseTime = new (1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => DateTime.Parse(reader.GetString());

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            var utc = value.ToUniversalTime();
            var valueToInsert = (utc.Ticks - _baseTime.Ticks) / 10000;
            writer.WriteNumberValue(valueToInsert);
        }
    }
}
