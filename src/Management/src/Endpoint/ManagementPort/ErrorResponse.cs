using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.ManagementPort;
internal class ErrorResponse
{
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("error")]
    public string Error { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; }

    [JsonPropertyName("path")]
    public string Path { get; set; }
}
