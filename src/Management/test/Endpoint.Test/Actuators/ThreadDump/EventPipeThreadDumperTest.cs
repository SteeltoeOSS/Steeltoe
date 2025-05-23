// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.Actuators.ThreadDump;

namespace Steeltoe.Management.Endpoint.Test.Actuators.ThreadDump;

public sealed class EventPipeThreadDumperTest
{
    [Trait("Category", "MemoryDumps")]
    [Fact]
    public async Task Can_resolve_source_location_from_pdb()
    {
        using var backgroundCancellationSource = new CancellationTokenSource();

        var backgroundThread = new Thread(NestedType.BackgroundThreadCallback)
        {
            IsBackground = true
        };

        backgroundThread.Start(backgroundCancellationSource.Token);

        using var loggerProvider = new CapturingLoggerProvider();
        using var loggerFactory = new LoggerFactory([loggerProvider]);
        ILogger<EventPipeThreadDumper> logger = loggerFactory.CreateLogger<EventPipeThreadDumper>();

        var optionsMonitor = new TestOptionsMonitor<ThreadDumpEndpointOptions>();
        var dumper = new EventPipeThreadDumper(optionsMonitor, logger);

        IList<ThreadInfo> threads = await dumper.DumpThreadsAsync(TestContext.Current.CancellationToken);

        StackTraceElement? backgroundThreadFrame = threads.SelectMany(thread => thread.StackTrace)
            .FirstOrDefault(frame => frame.MethodName == "BackgroundThreadCallback(class System.Object)");

        backgroundThreadFrame.Should().NotBeNull();
        backgroundThreadFrame.IsNativeMethod.Should().BeFalse();
        backgroundThreadFrame.ModuleName.Should().Be(GetType().Assembly.GetName().Name);
        backgroundThreadFrame.ClassName.Should().Be(typeof(NestedType).FullName);
        backgroundThreadFrame.FileName.Should().EndWith($"{nameof(EventPipeThreadDumperTest)}.cs");
        backgroundThreadFrame.LineNumber.Should().BeGreaterThan(0);
        backgroundThreadFrame.ColumnNumber.Should().BeGreaterThan(0);

        await backgroundCancellationSource.CancelAsync();
        backgroundThread.Join();

        IList<string> logLines = loggerProvider.GetAll();
        logLines.Should().Contain($"INFO {typeof(EventPipeThreadDumper).FullName}: Attempting to create a thread dump.");
        logLines.Should().Contain($"INFO {typeof(EventPipeThreadDumper).FullName}: Successfully created a thread dump.");

        string logText = loggerProvider.GetAsText();
        logText.Should().Contain($"TRCE {typeof(EventPipeThreadDumper).FullName}: Captured log from thread dump:");
        logText.Should().Contain("Created SymbolReader with SymbolPath");
    }

    [Fact]
    public async Task Includes_captured_log_for_thrown_exception()
    {
        var optionsMonitor = new TestOptionsMonitor<ThreadDumpEndpointOptions>();

        using var loggerProvider = new CapturingLoggerProvider();
        using var loggerFactory = new LoggerFactory([loggerProvider]);
        ILogger<EventPipeThreadDumper> logger = loggerFactory.CreateLogger<EventPipeThreadDumper>();

        var dumper = new EventPipeThreadDumper(optionsMonitor, logger);

        Func<Task> action = async () => await dumper.CaptureLogOutputAsync<IList<ThreadInfo>>(writer =>
        {
            writer.WriteLine("Failed to perform this operation.");
            throw new ArgumentException("Simulated failure.");
        }, TestContext.Current.CancellationToken);

        InvalidOperationException exception = (await action.Should().ThrowExactlyAsync<InvalidOperationException>()).Which;
        exception.Message.Should().StartWith($"Failed to create a thread dump. Captured log:{System.Environment.NewLine}Failed to perform this operation.");
        exception.InnerException.Should().BeOfType<ArgumentException>().Which.Message.Should().Be("Simulated failure.");
    }

    private static class NestedType
    {
        public static void BackgroundThreadCallback(object? argument)
        {
            var cancellationToken = (CancellationToken)argument!;

            while (!cancellationToken.IsCancellationRequested)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(50));
            }
        }
    }
}
