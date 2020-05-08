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

using Microsoft.Diagnostics.Runtime;
using Microsoft.Extensions.Logging;
using Steeltoe.Management.Diagnostics;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Steeltoe.Management.Endpoint.HeapDump
{
    public class WindowsHeapDumper : IHeapDumper
    {
        private readonly string _basePathOverride;

        private readonly ILogger<WindowsHeapDumper> _logger;
        private readonly IHeapDumpOptions _options;

        public WindowsHeapDumper(IHeapDumpOptions options, string basePathOverride = null, ILogger<WindowsHeapDumper> logger = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
            _basePathOverride = basePathOverride;
        }

        public string DumpHeap()
        {
            string fileName = CreateFileName();
            int curProcessId = Process.GetCurrentProcess().Id;
            Process process = Process.GetProcessById(curProcessId);

            IntPtr snapshotHandle = default;
            MiniDumper.Result result = default;

            try
            {
                int hr = LiveDataReader.PssCaptureSnapshot(
                        process.Handle,
                        GetCaptureFlags(),
                        IntPtr.Size == 8 ? 0x0010001F : 0x0001003F,
                        out snapshotHandle);

                if (hr != 0)
                {
                    _logger?.LogError(string.Format("Could not create snapshot to process. Error {0}.", hr));
                    return null;
                }

                if (_basePathOverride != null)
                {
                    fileName = _basePathOverride + fileName;
                }

                fileName = Path.GetFullPath(fileName);
                using var dumpFile = new FileStream(fileName, FileMode.Create);
                result = MiniDumper.DumpProcess(dumpFile, snapshotHandle, process.Id);
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Could not dump heap.");
                DeleteFileIfExists(fileName);
                fileName = null;
            }
            finally
            {
                if (snapshotHandle != IntPtr.Zero)
                {
                    int hr = LiveDataReader.PssQuerySnapshot(
                            snapshotHandle,
                            LiveDataReader.PSS_QUERY_INFORMATION_CLASS.PSS_QUERY_VA_CLONE_INFORMATION,
                            out IntPtr cloneQueryHandle,
                            IntPtr.Size);

                    if (hr == 0)
                    {
                        int clonePid = LiveDataReader.GetProcessId(cloneQueryHandle);
                        Process cloneProcess = Process.GetProcessById(clonePid);

                        hr = LiveDataReader.PssFreeSnapshot(process.Handle, snapshotHandle);
                        if (hr == 0)
                        {
                            try
                            {
                                cloneProcess.Kill();
                            }
                            catch (Exception e)
                            {
                                _logger?.LogError(e, "Could not kill clone pid");
                            }
                            finally
                            {
                                cloneProcess.Dispose();
                            }
                        }
                        else
                        {
                            _logger?.LogError(string.Format("Could not free the snapshot. Error {0}, Hr {1}.", Marshal.GetLastWin32Error(), hr));
                        }
                    }
                    else
                    {
                        _logger?.LogError(string.Format("Could not query the snapshot. Error {0}, Hr {1}.", Marshal.GetLastWin32Error(), hr));
                    }
                }

                process.Dispose();
            }

            if (result.ReturnValue == 0)
            {
                _logger?.LogError(
                    "MiniDump failed {hr}, {lastError}, {exception}",
                    result.ReturnValue,
                    result.ErrorCode,
                    result.Exception != null ? result.Exception.ToString() : string.Empty);
                return null;
            }

            return fileName;
        }

        internal LiveDataReader.PSS_CAPTURE_FLAGS GetCaptureFlags()
        {
            var flags = LiveDataReader.PSS_CAPTURE_FLAGS.PSS_CAPTURE_VA_CLONE |
                        LiveDataReader.PSS_CAPTURE_FLAGS.PSS_CAPTURE_HANDLES |
                        LiveDataReader.PSS_CAPTURE_FLAGS.PSS_CAPTURE_HANDLE_NAME_INFORMATION |
                        LiveDataReader.PSS_CAPTURE_FLAGS.PSS_CAPTURE_HANDLE_BASIC_INFORMATION |
                        LiveDataReader.PSS_CAPTURE_FLAGS.PSS_CAPTURE_HANDLE_TYPE_SPECIFIC_INFORMATION |
                        LiveDataReader.PSS_CAPTURE_FLAGS.PSS_CAPTURE_HANDLE_TRACE |
                        LiveDataReader.PSS_CAPTURE_FLAGS.PSS_CAPTURE_THREADS |
                        LiveDataReader.PSS_CAPTURE_FLAGS.PSS_CAPTURE_THREAD_CONTEXT |
                        LiveDataReader.PSS_CAPTURE_FLAGS.PSS_CREATE_MEASURE_PERFORMANCE;
            return flags;
        }

        internal string CreateFileName()
        {
            return "minidump-" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + "-live" + ".dmp";
        }

        private void DeleteFileIfExists(string fileName)
        {
            if (File.Exists(fileName))
            {
                try
                {
                    File.Delete(fileName);
                }
                catch (Exception e)
                {
                    _logger?.LogError(e, "Could not clean up dump file");
                }
            }
        }
    }
}
