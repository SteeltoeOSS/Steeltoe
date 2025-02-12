// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

    public string? DumpHeapToFile(CancellationToken cancellationToken)
    {
        string fileName = CreateFileName();

        try
        {
            int processId = System.Environment.ProcessId;

            if (string.Equals("gcdump", _optionsMonitor.CurrentValue.HeapDumpType, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Attempting to create a gcdump");

                if (TryCollectMemoryGraph(processId, 30, out MemoryGraph memoryGraph, cancellationToken))
                {
                    GCHeapDump.WriteMemoryGraph(memoryGraph, fileName, "dotnet-gcdump");
                    return fileName;
                }

                return null;
            }

            if (!Enum.TryParse(_optionsMonitor.CurrentValue.HeapDumpType, out DumpType dumpType))
            {
                dumpType = DumpType.Full;
            }

            _logger.LogInformation("Attempting to create a '{DumpType}' dump", dumpType);
            var client = new DiagnosticsClient(processId);
            client.WriteDump(dumpType, fileName);
            return fileName;
        }
        catch (DiagnosticsClientException exception)
        {
            _logger.LogError(exception, "Could not create core dump to process.");
            return null;
        }
    }

    private string CreateFileName()
    {
        DateTime utcNow = _timeProvider.GetUtcNow().UtcDateTime;

        return string.Equals("gcdump", _optionsMonitor.CurrentValue.HeapDumpType, StringComparison.OrdinalIgnoreCase)
            ? $"gcdump-{utcNow:yyyy-MM-dd-HH-mm-ss}-live.gcdump"
            : $"minidump-{utcNow:yyyy-MM-dd-HH-mm-ss}-live.dmp";
    }

    private bool TryCollectMemoryGraph(int processId, int timeout, out MemoryGraph memoryGraph, CancellationToken cancellationToken)
    {
        bool succeeded = false;
        using var logStream = new MemoryStream();

        using (TextWriter logWriter = new StreamWriter(logStream, leaveOpen: true))
        {
            var heapInfo = new DotNetHeapInfo();
            memoryGraph = new MemoryGraph(50_000);

            if (EventPipeDotNetHeapDumper.DumpFromEventPipe(cancellationToken, processId, memoryGraph, logWriter, timeout, heapInfo))
            {
                memoryGraph.AllowReading();
                succeeded = true;
            }
        }

        logStream.Seek(0, SeekOrigin.Begin);
        using var logReader = new StreamReader(logStream);
        string message = logReader.ReadToEnd();
        _logger.LogDebug("{HeapDumpLog}", message);

        return succeeded;
    }
}
