using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.Metrics
{
    public class MetricsResponseConverter : JsonConverter<IMetricsResponse>
    {
        public override IMetricsResponse Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, IMetricsResponse value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case MetricsListNamesResponse metricsList:
                    JsonSerializer.Serialize(writer, metricsList);
                    break;
                case MetricsResponse metricsResponse:
                    JsonSerializer.Serialize(writer, metricsResponse);
                    break;
            }
        }
    }
}