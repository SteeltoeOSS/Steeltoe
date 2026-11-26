// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.Actuators.LogFile;

/// <summary>
/// Describes which part of the log file to return. When no range is specified, the entire log file is returned.
/// </summary>
public sealed class LogFileRequest
{
    /// <summary>
    /// Gets the requested byte ranges. The default log file endpoint handler supports at most one range.
    /// </summary>
    [JsonPropertyName("ranges")]
    public IList<LogRange> Ranges { get; init; } = [];

    public override string ToString()
    {
        return Ranges.Count == 0 ? "Entire file" : $"bytes={string.Join(", ", Ranges)}";
    }
}
