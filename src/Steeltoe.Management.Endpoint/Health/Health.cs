using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Health
{
    [JsonConverter(typeof(HealthJsonConverter))]
    public class Health
    {
        public HealthStatus Status { get; set; } = HealthStatus.UNKNOWN;
        public string Description { get; set; }
        public Dictionary<string, object> Details { get; set; } = new Dictionary<string, object>();

    }

    public class HealthJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Health);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Health health = value as Health;
            writer.WriteStartObject();
            if (health != null)
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
