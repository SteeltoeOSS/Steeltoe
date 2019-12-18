// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.Logging;
using Steeltoe.Management.Diagnostics;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Steeltoe.Management.Endpoint.HeapDump
{
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