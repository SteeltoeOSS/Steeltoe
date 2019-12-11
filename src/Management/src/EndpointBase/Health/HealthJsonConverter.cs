// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Newtonsoft.Json;
using Steeltoe.Common.HealthChecks;
using System;

namespace Steeltoe.Management.Endpoint.Health
{
    public class HealthJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(HealthCheckResult);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            if (value is HealthCheckResult health)
            {
                writer.WritePropertyName("status");
                writer.WriteValue(health.Status.ToString());
                if (!string.IsNullOrEmpty(health.Description))
                {
                    writer.WritePropertyName("description");
                    writer.WriteValue(health.Description);
                }

                foreach (var detail in health.Details)
                {
                    writer.WritePropertyName(detail.Key);
                    serializer.Serialize(writer, detail.Value);
                }
            }

            writer.WriteEndObject();
        }
    }
}
