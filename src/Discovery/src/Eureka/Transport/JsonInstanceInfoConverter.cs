// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.AppInfo;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Steeltoe.Discovery.Eureka.Transport
{
    internal class JsonInstanceInfoConverter : JsonConverter<IList<InstanceInfo>>
    {
        public override IList<InstanceInfo> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var result = new List<InstanceInfo>();
            if (reader.TokenType.Equals(JsonTokenType.StartArray))
            {
                result = JsonSerializer.Deserialize<List<InstanceInfo>>(ref reader, options);
            }
            else
            {
                var singleInst = JsonSerializer.Deserialize<InstanceInfo>(ref reader, options);
                if (singleInst != null)
                {
                    result.Add(singleInst);
                }
            }

            return result;
        }

        public override void Write(Utf8JsonWriter writer, IList<InstanceInfo> value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }

        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(IList<InstanceInfo>);
        }
    }
}
