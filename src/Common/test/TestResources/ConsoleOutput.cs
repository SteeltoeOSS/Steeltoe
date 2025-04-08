// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using FluentAssertions.Extensions;

namespace Steeltoe.Common.TestResources;

/// <summary>
/// Captures the output of <see cref="Console.Out" />. Blocks concurrent access, so that tests can be run in parallel.
/// </summary>
/// <remarks>
/// Note this only works under the assumption that no other code is writing to Console.Out directly.
/// </remarks>
public sealed class ConsoleOutput : IDisposable
{
    private static readonly bool IsRunningOnBuildServer = Environment.GetEnvironmentVariable("CI") == "true" ||
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SYSTEM_TEAMPROJECT"));

    private static readonly SemaphoreSlim Lock = new(1, 1);

    private readonly TextWriter _backupWriter;
    private readonly StringBuilder _outputBuilder;
    private readonly StringWriter _outputWriter;

    private ConsoleOutput()
    {
        _backupWriter = Console.Out;
        _outputBuilder = new StringBuilder();
        _outputWriter = new StringWriter(_outputBuilder);

        Console.SetOut(_outputWriter);
    }

    public static ConsoleOutput Capture()
    {
        if (!Lock.Wait(TimeSpan.FromSeconds(5)))
        {
            throw new TimeoutException("Failed to obtain exclusive access to Console.Out.");
        }

        return new ConsoleOutput();
    }

    public void Clear()
    {
        _outputWriter.Flush();
        _outputBuilder.Clear();
    }

    public async Task WaitForFlushAsync(CancellationToken cancellationToken)
    {
        // Microsoft.Extensions.Logging.Console.ConsoleLogger writes messages to a queue,
        // it takes a bit of time for the background thread to write them to Console.Out.
        if (IsRunningOnBuildServer)
        {
            await Task.Delay(500.Milliseconds(), cancellationToken);
        }
        else
        {
            await Task.Delay(10.Milliseconds(), cancellationToken);
        }
    }

    public override string ToString()
    {
        _outputWriter.Flush();
        return _outputBuilder.ToString();
    }

    public void Dispose()
    {
        Console.SetOut(_backupWriter);
        _outputWriter.Dispose();
        Lock.Release();
    }
}
