// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.Actuators.LogFile;

/// <summary>
/// The outcome of a request for the log file. Dispose it after the content has been read.
/// </summary>
public sealed class LogFileResult : IDisposable
{
    internal static LogFileResult NotFound { get; } = new(LogFileResultStatus.NotFound, 0, 0, 0, null);

    /// <summary>
    /// Gets the outcome of the request.
    /// </summary>
    public LogFileResultStatus Status { get; }

    /// <summary>
    /// Gets the offset of the first byte in <see cref="Content" />, relative to the start of the log file.
    /// </summary>
    public long Offset { get; }

    /// <summary>
    /// Gets the number of bytes to read from <see cref="Content" />.
    /// </summary>
    public long Length { get; }

    /// <summary>
    /// Gets the length of the log file, in bytes, at the time it was opened.
    /// </summary>
    public long TotalSize { get; }

    /// <summary>
    /// Gets the stream to read the bytes from, positioned at <see cref="Offset" />. This is <c>null</c> unless <see cref="Status" /> is
    /// <see cref="LogFileResultStatus.Complete" /> or <see cref="LogFileResultStatus.Partial" />.
    /// </summary>
    public Stream? Content { get; }

    internal LogFileResult(LogFileResultStatus status, long offset, long length, long totalSize, Stream? content)
    {
        Status = status;
        Offset = offset;
        Length = length;
        TotalSize = totalSize;
        Content = content;
    }

    internal static LogFileResult CreateNotSatisfiable(long totalSize)
    {
        return new LogFileResult(LogFileResultStatus.RangeNotSatisfiable, 0, 0, totalSize, null);
    }

    public void Dispose()
    {
        Content?.Dispose();
    }
}
