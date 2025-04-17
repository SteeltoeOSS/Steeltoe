// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace Steeltoe.Management.Endpoint.Actuators.LogFile;

internal static class LogFileStreamer
{
    public static async IAsyncEnumerable<byte[]> ReadLogFileAsync(string fullPath, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, useAsync: true);
        byte[] buffer = new byte[4096];
        int bytesRead;

        while ((bytesRead = await fileStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            yield return buffer[..bytesRead];
        }
    }

    public static async IAsyncEnumerable<byte[]> ReadLogFileAsync(string fullPath, int startIndex, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, useAsync: true);
        byte[] buffer = new byte[4096];
        int bytesRead;
        // Seek to the start index
        fileStream.Seek(startIndex, SeekOrigin.Begin);
        while ((bytesRead = await fileStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            yield return buffer[..bytesRead];
        }
    }

    public static async IAsyncEnumerable<byte[]> ReadLogFileAsync(string fullPath, int startIndex, int endIndex, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, useAsync: true);
        byte[] buffer = new byte[4096];
        int bytesRead;
        int remainingBytes = endIndex - startIndex;

        // Seek to the start index
        fileStream.Seek(startIndex, SeekOrigin.Begin);

        while (remainingBytes > 0 && (bytesRead = await fileStream.ReadAsync(buffer.AsMemory(0, Math.Min(buffer.Length, remainingBytes)), cancellationToken)) > 0)
        {
            yield return buffer[..bytesRead];
            remainingBytes -= bytesRead;
        }
    }
}
