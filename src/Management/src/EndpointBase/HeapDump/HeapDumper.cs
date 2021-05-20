// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;

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
                if (!Enum.TryParse(typeof(DumpType), _options.HeapDumpType, out var dumpType))
                {
                    dumpType = DumpType.Full;
                }

                _logger?.LogInformation($"Attempting to create a '{dumpType}' heap dump");
                new DiagnosticsClient(Process.GetCurrentProcess().Id).WriteDump((DumpType)dumpType, fileName, false);
                return fileName;
            }
            catch (DiagnosticsClientException dcex)
            {
                _logger?.LogError(string.Format("Could not create core dump to process. Error {0}.", dcex));
                return null;
            }
        }

        internal string CreateFileName()
        {
            return "minidump-" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + "-live" + ".dmp";
        }
    }
}