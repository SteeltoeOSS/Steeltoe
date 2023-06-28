using System.Text.Json;
using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.Trace;
internal sealed class HttpTraceResultConverter : JsonConverter<HttpTraceResult>
{
    public override HttpTraceResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, HttpTraceResult value, JsonSerializerOptions options)
    {
        if (value is HttpTracesV2 tracesV2)
        {
            JsonSerializer.Serialize(writer, tracesV2, options);
        }
        else if(value is HttpTracesV1 tracesV1)
        {
            JsonSerializer.Serialize(writer, tracesV1, options);
        }
    }
}
