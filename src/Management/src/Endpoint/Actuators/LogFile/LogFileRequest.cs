// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.Actuators.LogFile;

/// <summary>
/// Describes which part of the log file to return. When no range is specified, the entire log file is returned.
/// </summary>
public sealed class LogFileRequest
{
    /// <summary>
    /// Gets the offset of the first byte to return, or <c>null</c> if the request is for the last <see cref="RangeEnd" /> bytes of the file, or for the
    /// entire file.
    /// </summary>
    public long? RangeStart { get; init; }

    /// <summary>
    /// Gets the offset of the last byte to return (inclusive), or <c>null</c> to return everything from <see cref="RangeStart" /> to the end of the file. When
    /// <see cref="RangeStart" /> is <c>null</c>, this is the number of bytes to return from the end of the file.
    /// </summary>
    public long? RangeEnd { get; init; }

    /// <summary>
    /// Gets a value indicating whether multiple ranges were requested, which is not supported.
    /// </summary>
    public bool HasMultipleRanges { get; init; }

    internal bool HasRange => HasMultipleRanges || RangeStart != null || RangeEnd != null;

    public override string ToString()
    {
        if (HasMultipleRanges)
        {
            return "Multiple ranges";
        }

        return HasRange ? $"Range={RangeStart}-{RangeEnd}" : "Entire file";
    }
}
