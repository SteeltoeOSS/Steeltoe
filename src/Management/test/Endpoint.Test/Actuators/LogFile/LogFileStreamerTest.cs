// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using Steeltoe.Common.TestResources.IO;
using Steeltoe.Management.Endpoint.Actuators.LogFile;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Logfile;

public sealed class LogFileStreamerTest
{
    [Fact]
    public async Task ReadLogFileAsync_ReturnsFileContentInChunks()
    {
        TempFile testLogFile = new();
        await using var writer = new StreamWriter(testLogFile.FullPath);
        // 12 KB of string data
        var sb = new StringBuilder();
        for (int i = 0; i < 4096 * 3; i++)
        {
            sb.Append((char)('a' + (i % 26)));
        }
        await writer.WriteAsync(sb.ToString());
        await writer.FlushAsync();

        var chunks = new List<byte[]>();

        await foreach (var chunk in LogFileStreamer.ReadLogFileAsync(testLogFile.FullPath, TestContext.Current.CancellationToken))
        {
            chunks.Add(chunk);
        }

        string result = string.Concat(chunks.Select(Encoding.UTF8.GetString));
        result.Should().Be(sb.ToString());
    }

    [Fact]
    public async Task ReadLogFileAsync_ReturnsFileContentFromStartingIndexToEnd()
    {
        TempFile testLogFile = new();
        await using var writer = new StreamWriter(testLogFile.FullPath);
        // 12 KB of string data
        var sb = new StringBuilder();
        for (int i = 0; i < 4096 * 3; i++)
        {
            sb.Append((char)('a' + (i % 26)));
        }
        await writer.WriteAsync(sb.ToString());
        await writer.FlushAsync();

        var chunks = new List<byte[]>();

        await foreach (var chunk in LogFileStreamer.ReadLogFileAsync(testLogFile.FullPath, 1025, TestContext.Current.CancellationToken))
        {
            chunks.Add(chunk);
        }

        string result = string.Concat(chunks.Select(Encoding.UTF8.GetString));
        result.Should().Be(sb.ToString()[1025..]);
    }

    [Fact]
    public async Task ReadLogFileAsync_ReturnsFileContentFromStartingIndexToEndIndex()
    {
        TempFile testLogFile = new();
        await using var writer = new StreamWriter(testLogFile.FullPath);
        // 12 KB of string data
        var sb = new StringBuilder();
        for (int i = 0; i < 4096 * 3; i++)
        {
            sb.Append((char)('a' + (i % 26)));
        }
        await writer.WriteAsync(sb.ToString());
        await writer.FlushAsync();

        var chunks = new List<byte[]>();

        await foreach (var chunk in LogFileStreamer.ReadLogFileAsync(testLogFile.FullPath, 1025, 1027, TestContext.Current.CancellationToken))
        {
            chunks.Add(chunk);
        }

        string result = string.Concat(chunks.Select(Encoding.UTF8.GetString));
        result.Should().Be(sb.ToString()[1025..1027]);
    }
}
