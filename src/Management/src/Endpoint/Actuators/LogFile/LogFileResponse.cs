// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.Actuators.LogFile;

/// <summary>
/// The outcome of a request for the log file. Dispose it after the content has been read.
/// </summary>
/// <param name="status">
/// The outcome of the request.
/// </param>
/// <param name="offset">
/// The offset of the first byte in <paramref name="content" />, relative to the start of the log file.
/// </param>
/// <param name="length">
/// The number of bytes to read from <paramref name="content" />.
/// </param>
/// <param name="totalSize">
/// The length of the log file, in bytes, at the time it was opened.
/// </param>
/// <param name="content">
/// The stream to read the bytes from, positioned at <paramref name="offset" />, or <c>null</c> when there is nothing to read.
/// </param>
public sealed class LogFileResponse(LogFileResponseStatus status, ulong offset, ulong length, ulong totalSize, Stream? content) : IDisposable
{
    internal static LogFileResponse NotFound { get; } = new(LogFileResponseStatus.NotFound, 0, 0, 0, null);

    /// <summary>
    /// Gets the outcome of the request.
    /// </summary>
    [JsonPropertyName("status")]
    public LogFileResponseStatus Status { get; } = status;

    /// <summary>
    /// Gets the offset of the first byte in <see cref="Content" />, relative to the start of the log file.
    /// </summary>
    [JsonPropertyName("offset")]
    public ulong Offset { get; } = offset;

    /// <summary>
    /// Gets the number of bytes to read from <see cref="Content" />.
    /// </summary>
    [JsonPropertyName("length")]
    public ulong Length { get; } = length;

    /// <summary>
    /// Gets the length of the log file, in bytes, at the time it was opened.
    /// </summary>
    [JsonPropertyName("totalSize")]
    public ulong TotalSize { get; } = totalSize;

    /// <summary>
    /// Gets the stream to read the bytes from, positioned at <see cref="Offset" />. This is <c>null</c> unless <see cref="Status" /> is
    /// <see cref="LogFileResponseStatus.Complete" /> or <see cref="LogFileResponseStatus.Partial" />.
    /// </summary>
    [JsonIgnore]
    public Stream? Content { get; } = content;

    internal static LogFileResponse CreateNotSatisfiable(ulong totalSize)
    {
        return new LogFileResponse(LogFileResponseStatus.RangeNotSatisfiable, 0, 0, totalSize, null);
    }

    public void Dispose()
    {
        Content?.Dispose();
    }
}
