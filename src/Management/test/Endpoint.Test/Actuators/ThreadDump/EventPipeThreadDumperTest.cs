// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.Actuators.ThreadDump;

namespace Steeltoe.Management.Endpoint.Test.Actuators.ThreadDump;

[Collection("TestsForMemoryDumpsMustRunSequentially")]
[Trait("Category", "MemoryDumps")]
public sealed class EventPipeThreadDumperTest
{
    [Fact]
    public async Task Can_resolve_source_location_from_pdb()
    {
        using var backgroundCancellationSource = new CancellationTokenSource();
        using var threadStarted = new ManualResetEventSlim(false);

        var backgroundThread = new Thread(NestedType.BackgroundThreadCallback)
        {
            IsBackground = true
        };

        backgroundThread.Start((backgroundCancellationSource.Token, threadStarted));
        threadStarted.Wait(TestContext.Current.CancellationToken);

        using var loggerProvider = new CapturingLoggerProvider();
        using var loggerFactory = new LoggerFactory([loggerProvider]);
        ILogger<EventPipeThreadDumper> logger = loggerFactory.CreateLogger<EventPipeThreadDumper>();

        var optionsMonitor = TestOptionsMonitor.Create(new ThreadDumpEndpointOptions
        {
            // For testing, sampling accuracy is more important than app pauses.
            Duration = 100
        });

        var dumper = new EventPipeThreadDumper(optionsMonitor, logger);

        StackTraceElement? callbackFrame = null;

        // A sampled instruction pointer occasionally lands inside a tiny CLR-injected sliver of the method (such as its GC-poll/safe-point check)
        // that has no direct IL-to-line mapping. The resolved source location then reports the correct file but line/column 0, instead of null,
        // because the lookup found the enclosing method but no matching line. Each dump takes an entirely new first sample, so a few retries make
        // this reliable without weakening what is actually being verified.
        for (int attempt = 0; attempt < 5 && callbackFrame?.LineNumber is null or 0; attempt++)
        {
            IList<ThreadInfo> results = await dumper.DumpThreadsAsync(TestContext.Current.CancellationToken);

            callbackFrame = results.SelectMany(thread => thread.StackTrace)
                .FirstOrDefault(frame => frame.MethodName == "BackgroundThreadCallback(class System.Object)");
        }

        if (callbackFrame == null)
        {
            string log = loggerProvider.GetAsText();
            throw new InvalidOperationException($"Failed to find expected stack frame. Captured log:{System.Environment.NewLine}{log}");
        }

        callbackFrame.IsNativeMethod.Should().BeFalse();
        callbackFrame.ModuleName.Should().Be(GetType().Assembly.GetName().Name);
        callbackFrame.ClassName.Should().Be(typeof(NestedType).FullName);
        callbackFrame.FileName.Should().EndWith($"{nameof(EventPipeThreadDumperTest)}.cs");
        callbackFrame.LineNumber.Should().BePositive();
        callbackFrame.ColumnNumber.Should().BePositive();

        await backgroundCancellationSource.CancelAsync();
        backgroundThread.Join();

        IList<string> logLines = loggerProvider.GetAll();
        logLines.Should().Contain($"INFO {typeof(EventPipeThreadDumper)}: Attempting to create a thread dump.");
        logLines.Should().Contain($"INFO {typeof(EventPipeThreadDumper)}: Successfully created a thread dump.");

        string logText = loggerProvider.GetAsText();
        logText.Should().Contain($"TRCE {typeof(EventPipeThreadDumper)}: Captured log from thread dump:");
        logText.Should().Contain("Created SymbolReader with SymbolPath");
    }

    [Fact]
    public async Task Includes_captured_log_for_thrown_exception()
    {
        var optionsMonitor = new TestOptionsMonitor<ThreadDumpEndpointOptions>();
        var dumper = new EventPipeThreadDumper(optionsMonitor, NullLogger<EventPipeThreadDumper>.Instance);

        Func<Task> action = async () => await dumper.CaptureLogOutputAsync<IList<ThreadInfo>>(writer =>
        {
            writer.WriteLine("Failed to perform this operation.");
            throw new ArgumentException("Simulated failure.");
        });

        await action.Should().ThrowExactlyAsync<InvalidOperationException>()
            .WithMessage($"Failed to create a thread dump. Captured log:{System.Environment.NewLine}Failed to perform this operation.")
            .WithInnerExceptionExactly<InvalidOperationException, ArgumentException>().WithMessage("Simulated failure.");
    }

    private static class NestedType
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void BackgroundThreadCallback(object? argument)
        {
            (CancellationToken cancellationToken, ManualResetEventSlim threadStarted) = ((CancellationToken, ManualResetEventSlim))argument!;

            threadStarted.Set();
            long counter = 0;

            // Only actively-running threads can be shown in the thread dump, so we need to ensure the CPU is in use. This must stay in pure managed code.
            while (!cancellationToken.IsCancellationRequested)
            {
                counter++;
#if NET8_0
                if (counter % 100_000 == 0)
                {
                    // Periodically yield to allow the EventPipe rundown thread to make progress on .NET 8, otherwise this thread can starve it of CPU time
                    // on constrained/busy machines (such as CI runners), making the dump take tens of seconds instead of a few hundred milliseconds.
                    // This is in native code, but yielding only occasionally (instead of every iteration) keeps the odds of a sample landing inside it low.
                    Thread.Sleep(0);
                }
#endif
            }

            _ = counter;
        }
    }
}
