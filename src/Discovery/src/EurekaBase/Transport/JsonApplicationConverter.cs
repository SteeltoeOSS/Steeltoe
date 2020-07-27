// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Steeltoe.Discovery.Eureka.Transport
{
    public class JsonApplicationConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IList<JsonApplication>);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            List<JsonApplication> result = null;
            try
            {
                if (reader.TokenType == JsonToken.StartArray)
                {
                    result = (List<JsonApplication>)serializer.Deserialize(reader, typeof(List<JsonApplication>));
                }
                else
                {
                    var singleInst = (JsonApplication)serializer.Deserialize(reader, typeof(JsonApplication));
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

            if (result == null)
            {
                result = new List<JsonApplication>();
            }

            return result;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}
