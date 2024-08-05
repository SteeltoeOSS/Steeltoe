// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Graphs;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tools.GCDump;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Steeltoe.Management.Endpoint.HeapDump;

public sealed class HeapDumper
{
    private readonly string? _basePathOverride;
    private readonly IOptionsMonitor<HeapDumpEndpointOptions> _optionsMonitor;
    private readonly ILogger<HeapDumper> _logger;

    public HeapDumper(IOptionsMonitor<HeapDumpEndpointOptions> optionsMonitor, ILogger<HeapDumper> logger)
        : this(optionsMonitor, null, logger)
    {
    }

    public HeapDumper(IOptionsMonitor<HeapDumpEndpointOptions> optionsMonitor, string? basePathOverride, ILogger<HeapDumper> logger)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(logger);

        _optionsMonitor = optionsMonitor;
        _logger = logger;
        _basePathOverride = basePathOverride;
    }

    public string? DumpHeapToFile(CancellationToken cancellationToken)
    {
        string fileName = CreateFileName();

        if (_basePathOverride != null)
        {
            fileName = _basePathOverride + fileName;
        }

        try
        {
            using var process = Process.GetCurrentProcess();

            if (System.Environment.Version.Major == 3 || string.Equals("gcdump", _optionsMonitor.CurrentValue.HeapDumpType, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Attempting to create a gcdump");

                if (TryCollectMemoryGraph(process.Id, 30, true, out MemoryGraph memoryGraph, cancellationToken))
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
            var client = new DiagnosticsClient(process.Id);
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
        if (System.Environment.Version.Major == 3 || string.Equals("gcdump", _optionsMonitor.CurrentValue.HeapDumpType, StringComparison.OrdinalIgnoreCase))
        {
            return $"gcdump-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}-live.gcdump";
        }

        return $"minidump-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}-live.dmp";
    }

    private bool TryCollectMemoryGraph(int processId, int timeout, bool verbose, out MemoryGraph memoryGraph, CancellationToken cancellationToken)
    {
        var heapInfo = new DotNetHeapInfo();
        TextWriter log = verbose ? Console.Out : TextWriter.Null;

        memoryGraph = new MemoryGraph(50_000);

        if (!EventPipeDotNetHeapDumper.DumpFromEventPipe(cancellationToken, processId, memoryGraph, log, timeout, heapInfo))
        {
            return false;
        }

        memoryGraph.AllowReading();
        return true;
    }
}
