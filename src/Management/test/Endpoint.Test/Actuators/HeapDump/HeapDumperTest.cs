// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.Actuators.HeapDump;

namespace Steeltoe.Management.Endpoint.Test.Actuators.HeapDump;

public sealed class HeapDumperTest
{
    private static readonly TimeSpan DumpTimeout = TimeSpan.FromMinutes(3);

    [Trait("Category", "MemoryDumps")]
    [Theory]
    [InlineData(HeapDumpType.Full, "fulldump_", "full dump")]
    [InlineData(HeapDumpType.Heap, "heapdump_", "dump with heap")]
    [InlineData(HeapDumpType.Mini, "minidump_", "minidump")]
    [InlineData(HeapDumpType.Triage, "triagedump_", "triage dump")]
    [InlineData(HeapDumpType.GCDump, "gcdump_", "gcdump")]
    public async Task Can_create_heap_dump(HeapDumpType heapDumpType, string fileName, string description)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && TestContext.Current.IsRunningOnBuildServer() && heapDumpType != HeapDumpType.GCDump)
        {
            // A heap dump triggers an OS-level popup on macOS, which blocks on build servers.
            return;
        }

        var optionsMonitor = TestOptionsMonitor.Create(new HeapDumpEndpointOptions
        {
            HeapDumpType = heapDumpType
        });

        using var loggerProvider = new CapturingLoggerProvider();
        using var loggerFactory = new LoggerFactory([loggerProvider]);
        ILogger<HeapDumper> logger = loggerFactory.CreateLogger<HeapDumper>();

        Task<string> dumpTask = Task.Run(() =>
        {
            var dumper = new HeapDumper(optionsMonitor, TimeProvider.System, logger);
            return dumper.DumpHeapToFile(TestContext.Current.CancellationToken);
        }, TestContext.Current.CancellationToken);

        string path = await dumpTask.WaitAsync(DumpTimeout, TestContext.Current.CancellationToken);

        path.Should().Contain(fileName);
        File.Delete(path);

        IList<string> logLines = loggerProvider.GetAll();
        logLines.Should().Contain($"INFO {typeof(HeapDumper).FullName}: Attempting to create a {description}.");
        logLines.Should().Contain($"INFO {typeof(HeapDumper).FullName}: Successfully created a {description}.");

        if (heapDumpType == HeapDumpType.GCDump)
        {
            string logText = loggerProvider.GetAsText();

            logText.Should().Contain($"TRCE {typeof(HeapDumper).FullName}: Captured log from gcdump:");
            logText.Should().Contain("Done Dumping .NET heap success=True");
        }
    }

    [Fact]
    public void Includes_captured_log_for_thrown_exception()
    {
        var optionsMonitor = TestOptionsMonitor.Create(new HeapDumpEndpointOptions
        {
            HeapDumpType = HeapDumpType.GCDump
        });

        var dumper = new HeapDumper(optionsMonitor, TimeProvider.System, NullLogger<HeapDumper>.Instance);

        Action action = () => dumper.CaptureLogOutput(writer =>
        {
            writer.WriteLine("Failed to perform this operation.");
            throw new ArgumentException("Simulated failure.");
        }, "gcdump", TestContext.Current.CancellationToken);

        action.Should().ThrowExactly<InvalidOperationException>()
            .WithMessage($"Failed to create a gcdump. Captured log:{System.Environment.NewLine}Failed to perform this operation.")
            .WithInnerException<ArgumentException>().WithMessage("Simulated failure.");
    }

    [Fact]
    public void Includes_captured_log_for_unsuccessful_action()
    {
        var optionsMonitor = TestOptionsMonitor.Create(new HeapDumpEndpointOptions
        {
            HeapDumpType = HeapDumpType.GCDump
        });

        var dumper = new HeapDumper(optionsMonitor, TimeProvider.System, NullLogger<HeapDumper>.Instance);

        Action action = () => dumper.CaptureLogOutput(writer =>
        {
            writer.WriteLine("Failed to perform this operation.");
            return false;
        }, "gcdump", TestContext.Current.CancellationToken);

        action.Should().ThrowExactly<InvalidOperationException>()
            .WithMessage($"Failed to create a gcdump. Captured log:{System.Environment.NewLine}Failed to perform this operation.").And.InnerException.Should()
            .BeNull();
    }
}
