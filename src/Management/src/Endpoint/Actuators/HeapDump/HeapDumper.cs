// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Graphs;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tools.GCDump;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Steeltoe.Management.Endpoint.Actuators.HeapDump;

internal sealed class HeapDumper : IHeapDumper
{
    private readonly IOptionsMonitor<HeapDumpEndpointOptions> _optionsMonitor;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<HeapDumper> _logger;

    public HeapDumper(IOptionsMonitor<HeapDumpEndpointOptions> optionsMonitor, TimeProvider timeProvider, ILogger<HeapDumper> logger)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _optionsMonitor = optionsMonitor;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public string DumpHeapToFile(CancellationToken cancellationToken)
    {
        HeapDumpEndpointOptions options = _optionsMonitor.CurrentValue;
        int processId = System.Environment.ProcessId;
        string? outputPath = null;

        string dumpDescription = options.HeapDumpType switch
        {
            HeapDumpType.GCDump => "gcdump",
            HeapDumpType.Heap => "dump with heap",
            HeapDumpType.Mini => "minidump",
            HeapDumpType.Triage => "triage dump",
            _ => "full dump"
        };

        _logger.LogInformation("Attempting to create a {DumpType}.", dumpDescription);

        try
        {
            outputPath = GetOutputPath(options.HeapDumpType);

            if (options.HeapDumpType == HeapDumpType.GCDump)
            {
                CreateGCDump(processId, outputPath, dumpDescription, options.GCDumpTimeoutInSeconds, cancellationToken);
            }
            else
            {
                CreateHeapDump(options.HeapDumpType, processId, outputPath);
            }
        }
        catch (Exception)
        {
            SafeDelete(outputPath);
            throw;
        }

        _logger.LogInformation("Successfully created a {DumpType}.", dumpDescription);
        return outputPath;
    }

    private string GetOutputPath(HeapDumpType? heapDumpType)
    {
        DateTime utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        string timestamp = $"{utcNow:yyyyMMdd_HHmmss}";

        string name = heapDumpType switch
        {
            HeapDumpType.Heap => "heapdump",
            HeapDumpType.Mini => "minidump",
            HeapDumpType.Triage => "triagedump",
            HeapDumpType.GCDump => "gcdump",
            _ => "fulldump"
        };

        string extension = string.Empty;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            extension = heapDumpType == HeapDumpType.GCDump ? ".gcdump" : ".dmp";
        }

        string fileName = $"{name}_{timestamp}{extension}";
        return Path.GetFullPath(Path.Combine(Path.GetTempPath(), fileName));
    }

    private void CreateGCDump(int processId, string outputPath, string dumpDescription, int timeoutInSeconds, CancellationToken cancellationToken)
    {
        CaptureLogOutput(logWriter =>
        {
            var heapInfo = new DotNetHeapInfo();
            var memoryGraph = new MemoryGraph(50_000);

            if (EventPipeDotNetHeapDumper.DumpFromEventPipe(cancellationToken, processId, null, memoryGraph, logWriter, timeoutInSeconds, heapInfo))
            {
                memoryGraph.AllowReading();
                GCHeapDump.WriteMemoryGraph(memoryGraph, outputPath, "dotnet-gcdump");
                return true;
            }

            return false;
        }, dumpDescription, cancellationToken);
    }

    private static void CreateHeapDump(HeapDumpType? heapDumpType, int processId, string outputPath)
    {
        var client = new DiagnosticsClient(processId);

        DumpType dumpType = heapDumpType switch
        {
            HeapDumpType.Heap => DumpType.WithHeap,
            HeapDumpType.Mini => DumpType.Normal,
            HeapDumpType.Triage => DumpType.Triage,
            _ => DumpType.Full
        };

#pragma warning disable S3265 // Non-flags enums should not be used in bitwise operations
        // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
        const WriteDumpFlags flags = WriteDumpFlags.LoggingEnabled | WriteDumpFlags.CrashReportEnabled;
#pragma warning restore S3265 // Non-flags enums should not be used in bitwise operations

        client.WriteDump(dumpType, outputPath, flags);
    }

    private void CaptureLogOutput(Func<TextWriter, bool> action, string dumpDescription, CancellationToken cancellationToken)
    {
        using var logStream = new MemoryStream();
        Exception? error = null;
        bool succeeded = false;

        using (TextWriter logWriter = new StreamWriter(logStream, leaveOpen: true))
        {
            try
            {
                succeeded = action(logWriter);
            }
            catch (Exception exception)
            {
                error = exception;
            }
        }

        logStream.Seek(0, SeekOrigin.Begin);
        using var logReader = new StreamReader(logStream);
        string logOutput = logReader.ReadToEnd();

        if (error != null || !succeeded)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException($"Failed to create a {dumpDescription}. Captured log:{System.Environment.NewLine}{logOutput}", error);
        }

        _logger.LogTrace("Captured log from {DumpType}:{LineBreak}{DumpLog}", dumpDescription, System.Environment.NewLine, logOutput);
    }

    private static void SafeDelete(string? outputPath)
    {
        if (outputPath != null)
        {
            try
            {
                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }
            }
            catch (Exception)
            {
                // Intentionally left empty.
            }
        }
    }
}
