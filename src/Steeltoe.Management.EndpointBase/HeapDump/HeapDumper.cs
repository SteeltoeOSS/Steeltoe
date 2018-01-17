// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Diagnostics.Runtime;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Steeltoe.Management.Endpoint.HeapDump
{
    public class HeapDumper : IHeapDumper
    {
        private const int PROCESS_VM_READ = 0x10;
        private const int PROCESS_QUERY_INFORMATION = 0x0400;

        private ILogger<HeapDumper> _logger;
        private IHeapDumpOptions _options;

        public HeapDumper(IHeapDumpOptions options, ILogger<HeapDumper> logger = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
        }

        public string DumpHeap()
        {
            string fileName = CreateFileName();

            Process process = Process.GetProcessById(Process.GetCurrentProcess().Id);

            IntPtr cloneQueryHandle = default(IntPtr);
            IntPtr snapshotHandle = default(IntPtr);
            int clonePid = -1;
            IntPtr cloneProcessHandle = default(IntPtr);

            try
            {
                int hr = LiveDataReader.PssCaptureSnapshot(
                        process.Handle,
                        LiveDataReader.PSS_CAPTURE_FLAGS.PSS_CAPTURE_VA_CLONE,
                        IntPtr.Size == 8 ? 0x0010001F : 0x0001003F,
                        out snapshotHandle);

                if (hr != 0)
                {
                    _logger.LogError(string.Format("Could not create snapshot to process. Error {0}.", hr));
                    return null;
                }

                hr = LiveDataReader.PssQuerySnapshot(
                    snapshotHandle,
                    LiveDataReader.PSS_QUERY_INFORMATION_CLASS.PSS_QUERY_VA_CLONE_INFORMATION,
                    out cloneQueryHandle,
                    IntPtr.Size);

                if (hr != 0)
                {
                    _logger.LogError(string.Format("Could not query the snapshot. Error {0}.", hr));
                    return null;
                }

                clonePid = LiveDataReader.GetProcessId(cloneQueryHandle);
                cloneProcessHandle = LiveDataReader.OpenProcess(PROCESS_VM_READ | PROCESS_QUERY_INFORMATION, false, clonePid);

                if (cloneProcessHandle == IntPtr.Zero)
                {
                    _logger.LogError(string.Format("Could not attach to cloned process. Error {0}.", Marshal.GetLastWin32Error()));
                    return null;
                }

                string fullPath = Path.GetFullPath(fileName);
                FileStream dumpFile = new FileStream(fullPath, FileMode.CreateNew);
                var fileHandle = dumpFile.SafeFileHandle.DangerousGetHandle();
                if (!MiniDumpWriteDump(cloneProcessHandle, clonePid, fileHandle, MINIDUMP_TYPE.MiniDumpNormal, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero))
                {
                    _logger.LogError("MiniDumpWriteDump failed");
                    return null;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not dump heap.");
                DeleteFileIfExists(fileName);
                fileName = null;
            }
            finally
            {
                if (cloneQueryHandle != IntPtr.Zero)
                {
                    LiveDataReader.CloseHandle(cloneQueryHandle);  // Causes SEH Exceptions?
                    if (snapshotHandle != IntPtr.Zero)
                    {
                        int hr = LiveDataReader.PssFreeSnapshot(Process.GetCurrentProcess().Handle, snapshotHandle);
                        if (hr != 0)
                        {
                            _logger.LogError(string.Format("Could not free the snapshot. Error {0}.", hr));
                        }
                    }

                    try
                    {
                        if (clonePid != -1)
                        {
                            Process.GetProcessById(clonePid).Kill();
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Could not kill clone pid");
                    }
                }

                if (cloneProcessHandle != IntPtr.Zero)
                {
                    LiveDataReader.CloseHandle(cloneProcessHandle);
                }
            }

            return fileName;
        }

        internal string CreateFileName()
        {
            return "minidump-" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm") + "-live" + ".dump";
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
                    _logger.LogError(e, "Could not clean up dump file");
                }
            }
        }

        private enum MINIDUMP_TYPE : uint
        {
            MiniDumpNormal = 0x00000000,
            MiniDumpWithDataSegs = 0x00000001,
            MiniDumpWithFullMemory = 0x00000002,
            MiniDumpWithHandleData = 0x00000004,
            MiniDumpFilterMemory = 0x00000008,
            MiniDumpScanMemory = 0x00000010,
            MiniDumpWithUnloadedModules = 0x00000020,
            MiniDumpWithIndirectlyReferencedMemory = 0x00000040,
            MiniDumpFilterModulePaths = 0x00000080,
            MiniDumpWithProcessThreadData = 0x00000100,
            MiniDumpWithPrivateReadWriteMemory = 0x00000200,
            MiniDumpWithoutOptionalData = 0x00000400,
            MiniDumpWithFullMemoryInfo = 0x00000800,
            MiniDumpWithThreadInfo = 0x00001000,
            MiniDumpWithCodeSegs = 0x00002000,
            MiniDumpWithoutAuxiliaryState = 0x00004000,
            MiniDumpWithFullAuxiliaryState = 0x00008000,
            MiniDumpWithPrivateWriteCopyMemory = 0x00010000,
            MiniDumpIgnoreInaccessibleMemory = 0x00020000,
            MiniDumpWithTokenInformation = 0x00040000,
            MiniDumpWithModuleHeaders = 0x00080000,
            MiniDumpFilterTriage = 0x00100000,
        }

        [DllImport("DbgHelp")]
        private static extern bool MiniDumpWriteDump(IntPtr processHandle, int processId, IntPtr fileHandle, MINIDUMP_TYPE dumpType, IntPtr excepParam, IntPtr userParam, IntPtr callParam);
    }
}
