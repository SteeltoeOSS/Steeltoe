// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Graphs;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tools.GCDump;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Steeltoe.Management.Endpoint.HeapDump
{
    public class HeapDumper : IHeapDumper
    {
        private readonly string _basePathOverride;
        private readonly ILogger<HeapDumper> _logger;
        private readonly IHeapDumpOptions _options;

        public HeapDumper(IHeapDumpOptions options, string basePathOverride = null, ILogger<HeapDumper> logger = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
            _basePathOverride = basePathOverride;
        }

        public string DumpHeap()
        {
            var fileName = CreateFileName();
            if (_basePathOverride != null)
            {
                fileName = _basePathOverride + fileName;
            }

            try
            {
                if (Environment.Version.Major == 3 || "gcdump".Equals(_options.HeapDumpType, StringComparison.OrdinalIgnoreCase))
                {
                    _logger?.LogInformation("Attempting to create a gcdump");
                    if (TryCollectMemoryGraph(CancellationToken.None, Process.GetCurrentProcess().Id, 30, true, out var memoryGraph))
                    {
                        GCHeapDump.WriteMemoryGraph(memoryGraph, fileName, "dotnet-gcdump");
                        return fileName;
                    }

                    return null;
                }

                if (!Enum.TryParse(typeof(DumpType), _options.HeapDumpType, out var dumpType))
                {
                    dumpType = DumpType.Full;
                }

                _logger?.LogInformation($"Attempting to create a '{dumpType}' dump");
                new DiagnosticsClient(Process.GetCurrentProcess().Id).WriteDump((DumpType)dumpType, fileName);
                return fileName;
            }
            catch (DiagnosticsClientException dcex)
            {
                _logger?.LogError($"Could not create core dump to process. Error {dcex}.");
                return null;
            }
        }

        internal string CreateFileName()
        {
            if (Environment.Version.Major == 3 || "gcdump".Equals(_options.HeapDumpType, StringComparison.OrdinalIgnoreCase))
            {
                return $"gcdump-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}-live.gcdump";
            }
            else
            {
                return $"minidump-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}-live.dmp";
            }
        }

        internal bool TryCollectMemoryGraph(CancellationToken ct, int processId, int timeout, bool verbose,  out MemoryGraph memoryGraph)
        {
            var heapInfo = new DotNetHeapInfo();
            var log = verbose ? Console.Out : TextWriter.Null;

            memoryGraph = new MemoryGraph(50_000);

            if (!EventPipeDotNetHeapDumper.DumpFromEventPipe(ct, processId, memoryGraph, log, timeout, heapInfo))
            {
                return false;
            }

            memoryGraph.AllowReading();
            return true;
        }
    }
}