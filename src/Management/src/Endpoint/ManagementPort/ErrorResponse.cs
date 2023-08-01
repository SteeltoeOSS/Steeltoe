// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.ManagementPort;

internal sealed class ErrorResponse
{
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; }

    [JsonPropertyName("status")]
    public int Status { get; }

    [JsonPropertyName("error")]
    public string Error { get; }

    [JsonPropertyName("message")]
    public string Message { get; }

    [JsonPropertyName("path")]
    public string Path { get; }

    public ErrorResponse(DateTime timestamp, int status, string error, string message, string path)
    {
        Timestamp = timestamp;
        Status = status;
        Error = error;
        Message = message;
        Path = path;
    }
}
