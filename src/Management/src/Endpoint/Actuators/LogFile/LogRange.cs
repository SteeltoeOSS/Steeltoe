// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.Actuators.LogFile;

/// <summary>
/// Describes a range of bytes in the log file, with the semantics of an HTTP byte range.
/// </summary>
/// <param name="Start">
/// The offset of the first byte to return, or <c>null</c> to return the last <paramref name="End" /> bytes of the file.
/// </param>
/// <param name="End">
/// The offset of the last byte to return (inclusive), or <c>null</c> to return everything from <paramref name="Start" /> to the end of the file. When
/// <paramref name="Start" /> is <c>null</c>, this is the number of bytes to return from the end of the file.
/// </param>
public sealed record LogRange(
    [property: JsonPropertyName("start")] ulong? Start,
    [property: JsonPropertyName("end")] ulong? End)
{
    /// <summary>
    /// Determines which bytes of a file of the specified size are covered by this range.
    /// </summary>
    /// <param name="totalSize">
    /// The size of the file, in bytes.
    /// </param>
    /// <param name="start">
    /// When this method returns <c>true</c>, the offset of the first byte.
    /// </param>
    /// <param name="end">
    /// When this method returns <c>true</c>, the offset of the last byte (inclusive).
    /// </param>
    /// <returns>
    /// <c>true</c> if the range overlaps with the file, or <c>false</c> if it is invalid or not satisfiable.
    /// </returns>
    public bool TryGetByteRange(ulong totalSize, out ulong start, out ulong end)
    {
        start = 0;
        end = 0;

        if (Start == null)
        {
            // Suffix range, such as "bytes=-500", which requests the last 500 bytes.
            ulong suffixLength = End.GetValueOrDefault();

            if (suffixLength == 0 || totalSize == 0)
            {
                return false;
            }

            start = suffixLength >= totalSize ? 0 : totalSize - suffixLength;
            end = totalSize - 1;
            return true;
        }

        if (Start >= totalSize || End < Start)
        {
            return false;
        }

        start = Start.Value;
        end = End == null ? totalSize - 1 : Math.Min(End.Value, totalSize - 1);
        return true;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return string.Create(CultureInfo.InvariantCulture, $"{Start}-{End}");
    }
}
