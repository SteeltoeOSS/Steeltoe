// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;

namespace Steeltoe.Management.Endpoint.HeapDump
{
    [Obsolete("This class will be removed in a future release. Use HeapDumper instead")]
    public class LinuxHeapDumper : IHeapDumper
    {
        private readonly string _basePathOverride;
        private readonly ILogger<LinuxHeapDumper> _logger;
        private readonly IHeapDumpOptions _options;

        public LinuxHeapDumper(IHeapDumpOptions options, string basePathOverride = null, ILogger<LinuxHeapDumper> logger = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
            _basePathOverride = basePathOverride;
        }

        public string DumpHeap()
        {
            string fileName = CreateFileName();
            if (_basePathOverride != null)
            {
                fileName = _basePathOverride + fileName;
            }

            try
            {
                // TODO: Honor option with respect to dump type (how? - IHeapDumpOptions don't seems to have the information)
                new DiagnosticsClient(Process.GetCurrentProcess().Id).WriteDump(DumpType.Full, fileName, false);
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