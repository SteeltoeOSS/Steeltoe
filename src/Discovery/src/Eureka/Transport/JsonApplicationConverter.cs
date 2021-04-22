// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.AppInfo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Steeltoe.Discovery.Eureka.Transport
{
    internal class JsonApplicationConverter : JsonConverter<IEnumerable<Application>>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(IEnumerable<Application>);
        }

        public override IEnumerable<Application> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var result = Enumerable.Empty<Application>();

            try
            {
                if (reader.TokenType == JsonTokenType.StartArray)
                {
                    result = JsonSerializer.Deserialize<IEnumerable<Application>>(ref reader, options);
                }
                else
                {
                    var singleInst = JsonSerializer.Deserialize<Application>(ref reader, options);
                    if (singleInst != null)
                    {
                        result = new List<Application>
                        {
                            singleInst
                        };
                    }
                }
            }
            catch (Exception)
            {
                result = Enumerable.Empty<Application>();
            }

            return result;
        }

        public override void Write(Utf8JsonWriter writer, IEnumerable<Application> value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
