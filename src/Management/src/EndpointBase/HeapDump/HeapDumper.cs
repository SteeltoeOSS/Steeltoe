// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Runtime;
using Microsoft.Extensions.Logging;
using Steeltoe.Management.Diagnostics;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

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
            var curProcessId = Process.GetCurrentProcess().Id;
            var process = Process.GetProcessById(curProcessId);

            var snapshotHandle = default(IntPtr);
            var result = default(MiniDumper.Result);

            try
            {
                var hr = LiveDataReader.PssCaptureSnapshot(
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
                    var hr = LiveDataReader.PssQuerySnapshot(
                            snapshotHandle,
                            LiveDataReader.PSS_QUERY_INFORMATION_CLASS.PSS_QUERY_VA_CLONE_INFORMATION,
                            out var cloneQueryHandle,
                            IntPtr.Size);

                    if (hr == 0)
                    {
                        var clonePid = LiveDataReader.GetProcessId(cloneQueryHandle);
                        var cloneProcess = Process.GetProcessById(clonePid);

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
