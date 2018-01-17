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
using Microsoft.DiaSymReader;
using Microsoft.DiaSymReader.PortablePdb;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Steeltoe.Management.Endpoint.ThreadDump
{
    public class ThreadDumper : IThreadDumper
    {
        private ILogger<ThreadDumper> _logger;
        private IThreadDumpOptions _options;
        private Dictionary<PdbInfo, ISymUnmanagedReader> _pdbReaders = new Dictionary<PdbInfo, ISymUnmanagedReader>();

        public ThreadDumper(IThreadDumpOptions options, ILogger<ThreadDumper> logger = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
        }

        public List<ThreadInfo> DumpThreads()
        {
            List<ThreadInfo> results = new List<ThreadInfo>();

            DataTarget dataTarget = CreateDataTarget();
            if (dataTarget != null)
            {
                try
                {
                    _logger?.LogDebug("Starting thread dump");
                    DumpThreads(dataTarget, results);
                }
                catch (Exception e)
                {
                    _logger?.LogError(e, "Unable to dump threads");
                }
                finally
                {
                    DisposeSymbolReaders();
                    dataTarget.Dispose();
                    long totalMemory = GC.GetTotalMemory(true);
                    _logger?.LogInformation("Total Memory {0}", totalMemory);
                }
            }

            return results;
        }

        private void DisposeSymbolReaders()
        {
            foreach (var reader in _pdbReaders)
            {
                var symReader = reader.Value;
                try
                {
                    Marshal.FinalReleaseComObject(symReader);
                }
                catch (Exception e)
                {
                    _logger?.LogDebug(e, "Failed to release SymReader");
                }
            }

            _pdbReaders.Clear();
        }

        private DataTarget CreateDataTarget()
        {
            try
            {
                return DataTarget.CreateSnapshotAndAttach(Process.GetCurrentProcess().Id);
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Unable to create process snapshot");
            }

            return null;
        }

        private void DumpThreads(DataTarget dataTarget, List<ThreadInfo> results)
        {
            var runtime = dataTarget.ClrVersions[0].CreateRuntime();
            if (runtime != null)
            {
                IList<BlockingObject> allLocks = GetAllLocks(runtime);

                _logger?.LogTrace("Starting thread walk");

                foreach (var thread in runtime.Threads)
                {
                    if (thread.IsAlive)
                    {
                        ThreadInfo threadInfo = new ThreadInfo()
                        {
                            ThreadId = thread.ManagedThreadId,
                            ThreadName = GetThreadName(thread)
                        };

                        threadInfo.ThreadState = GetThreadState(thread);
                        threadInfo.StackTrace = GetStackTrace(thread);
                        threadInfo.IsInNative = IsInNative(threadInfo.StackTrace);
                        threadInfo.IsSuspended = IsUserSuspended(thread);
                        threadInfo.LockedMonitors = GetThisThreadsLockedMonitors(thread, allLocks);
                        threadInfo.LockedSynchronizers = GetThisThreadsLockedSyncronizers(thread, allLocks);
                        UpdateWithLockInfo(thread, threadInfo);
                        results.Add(threadInfo);
                    }
                }

                _logger?.LogTrace("Finished thread walk");
            }
        }

        private void UpdateWithLockInfo(ClrThread thread, ThreadInfo info)
        {
            var locks = thread.BlockingObjects;
            if (locks != null)
            {
                foreach (var lck in locks)
                {
                    if (lck.Taken)
                    {
                        var lockClass = thread.Runtime.Heap.GetObjectType(lck.Object);
                        if (lockClass != null)
                        {
                            LockInfo lockInfo = null;
                            if (lck.Reason == BlockingReason.Monitor ||
                                lck.Reason == BlockingReason.MonitorWait)
                            {
                                lockInfo = new MonitorInfo();
                            }
                            else
                            {
                                lockInfo = new LockInfo();
                            }

                            lockInfo.ClassName = lockClass.Name;
                            lockInfo.IdentityHashCode = (int)lck.Object;

                            info.LockInfo = lockInfo;
                            info.LockName = lockInfo.ClassName + "@" + string.Format("{0:X}", lck.Object);
                            info.LockOwnerId = lck.HasSingleOwner && lck.Owner != null ? lck.Owner.ManagedThreadId : -1;
                            info.LockOwnerName = lck.HasSingleOwner && lck.Owner != null ? GetThreadName(lck.Owner) : "Unknown";
                        }
                    }
                }
            }

            _logger?.LogTrace("Updated threads lock info");
        }

        private List<LockInfo> GetThisThreadsLockedSyncronizers(ClrThread thread, IList<BlockingObject> allLocks)
        {
            List<LockInfo> result = new List<LockInfo>();
            foreach (var lck in allLocks)
            {
                if (lck.Reason != BlockingReason.Monitor &&
                    lck.Reason != BlockingReason.MonitorWait)
                {
                    if (thread.Address == lck.Owner?.Address)
                    {
                        var lockClass = thread.Runtime.Heap.GetObjectType(lck.Object);
                        if (lockClass != null)
                        {
                            LockInfo info = new LockInfo()
                            {
                                ClassName = lockClass.Name,
                                IdentityHashCode = (int)lck.Object
                            };
                            result.Add(info);
                        }
                    }
                }
            }

            _logger?.LogTrace("Thread has {0} non monitor locks", result.Count);
            return result;
        }

        private List<MonitorInfo> GetThisThreadsLockedMonitors(ClrThread thread, IList<BlockingObject> allLocks)
        {
            List<MonitorInfo> result = new List<MonitorInfo>();

            foreach (var lck in allLocks)
            {
                if (lck.Reason == BlockingReason.Monitor ||
                    lck.Reason == BlockingReason.MonitorWait)
                {
                    if (thread.Address == lck.Owner?.Address)
                    {
                        var lockClass = thread.Runtime.Heap.GetObjectType(lck.Object);
                        if (lockClass != null)
                        {
                            MonitorInfo info = new MonitorInfo()
                            {
                                ClassName = lockClass.Name,
                                IdentityHashCode = (int)lck.Object
                            };
                            result.Add(info);
                        }
                    }
                }
            }

            _logger?.LogTrace("Thread has {0} monitor locks", result.Count);
            return result;
        }

        private bool IsUserSuspended(ClrThread thread)
        {
            _logger?.LogTrace("Thread user suspended {0}", thread.IsUserSuspended);
            return thread.IsUserSuspended;
        }

        private bool IsInNative(List<StackTraceElement> elements)
        {
            bool result = false;
            if (elements.Count > 0)
            {
                result = elements[0].IsNativeMethod;
            }

            _logger?.LogTrace("Thread in native method {0}", result);
            return result;
        }

        private List<StackTraceElement> GetStackTrace(ClrThread thread)
        {
            List<StackTraceElement> result = new List<StackTraceElement>();

            _logger?.LogTrace("Starting stack dump for thread");

            if (thread.IsAlive)
            {
                var stackTrace = thread.StackTrace;
                if (stackTrace != null)
                {
                    foreach (ClrStackFrame frame in stackTrace)
                    {
                        StackTraceElement element = GetStackTraceElement(frame);
                        if (element != null)
                        {
                            result.Add(element);
                        }
                    }
                }
            }

            return result;
        }

        private StackTraceElement GetStackTraceElement(ClrStackFrame frame)
        {
            if (frame == null || frame.Method == null)
            {
                return null;
            }

            FileAndLineNumber info = GetSourceLocation(frame);
            StackTraceElement result = new StackTraceElement()
            {
                MethodName = frame.Method.Name,
                IsNativeMethod = frame.Method.IsInternal || frame.Method.IsPInvoke,
                LineNumber = info.Line,
                FileName = info.File,
                ClassName = frame.Method.Type != null ? frame.Method.Type.Name : "Unknown"
            };

            return result;
        }

        private TState GetThreadState(ClrThread thread)
        {
            TState result = TState.NEW;
            if (thread.IsUnstarted)
            {
                return result;
            }

            if (!thread.IsAlive)
            {
                return TState.TERMINATED;
            }

            result = TState.RUNNABLE;
            if (thread.IsBlocked)
            {
                result = TState.TIMED_WAITING;
                if (IsWaitingOnMonitor(thread))
                {
                    result = TState.BLOCKED;
                }
                else if (IsWaitingOnNonMonitor(thread))
                {
                    result = TState.WAITING;
                }
            }

            _logger?.LogTrace("Thread state {0}", result);
            return result;
        }

        private bool IsWaitingOnMonitor(ClrThread thread)
        {
            bool result = false;
            if (thread.BlockingObjects != null)
            {
                foreach (var lck in thread.BlockingObjects)
                {
                    if (lck.Taken)
                    {
                        if (lck.Reason == BlockingReason.Monitor ||
                            lck.Reason == BlockingReason.MonitorWait)
                        {
                            result = true;
                        }
                    }
                }
            }

            return result;
        }

        private bool IsWaitingOnNonMonitor(ClrThread thread)
        {
            bool result = false;
            if (thread.BlockingObjects != null)
            {
                foreach (var lck in thread.BlockingObjects)
                {
                    if (lck.Taken)
                    {
                        if (lck.Reason != BlockingReason.Monitor &&
                            lck.Reason != BlockingReason.MonitorWait)
                        {
                            result = true;
                        }
                    }
                }
            }

            return result;
        }

        private string GetThreadName(ClrThread thread)
        {
            int id = thread.ManagedThreadId;
            string name = "Thread-" + id;

            if (thread.IsFinalizer)
            {
                name = "Finalizer-" + name;
            }
            else if (thread.IsThreadpoolWorker)
            {
                name = "PoolWorker-" + name;
            }
            else if (thread.IsBackground)
            {
                name = "Background-" + name;
            }

            _logger?.LogTrace("Processing thread {0}", name);
            return name;
        }

        private IList<BlockingObject> GetAllLocks(ClrRuntime runtime)
        {
            List<BlockingObject> locks = new List<BlockingObject>();

            _logger?.LogTrace("Starting lock walk");
            foreach (var thread in runtime.Threads)
            {
                var blocking = thread.BlockingObjects;
                if (blocking != null)
                {
                    foreach (var lck in blocking)
                    {
                        if (lck.Taken)
                        {
                            locks.Add(lck);
                        }
                    }
                }
            }

            return locks;
        }

        internal struct FileAndLineNumber
        {
            public string File;
            public int Line;
        }

        private FileAndLineNumber GetSourceLocation(ClrStackFrame frame)
        {
            var reader = GetReaderForFrame(frame);
            if (reader != null)
            {
                var method = frame.Method;

                var result = reader.GetMethod((int)method.MetadataToken, out ISymUnmanagedMethod methodSym);
                if (methodSym != null)
                {
                    var seqPoints = methodSym.GetSequencePoints();
                    var ilOffset = FindIlOffset(frame);
                    if (ilOffset >= 0)
                    {
                        return FindNearestLine(seqPoints, ilOffset);
                    }
                }
            }

            return default(FileAndLineNumber);
        }

        private FileAndLineNumber FindNearestLine(IEnumerable<SymUnmanagedSequencePoint> sequences, int ilOffset)
        {
            int distance = int.MaxValue;
            FileAndLineNumber nearest = default(FileAndLineNumber);

            foreach (var point in sequences)
            {
                int dist = (int)Math.Abs(point.Offset - ilOffset);
                if (dist < distance)
                {
                    nearest.File = Path.GetFileName(point.Document.GetName());
                    nearest.Line = (int)point.StartLine;
                }

                distance = dist;
            }

            return nearest;
        }

        private int FindIlOffset(ClrStackFrame frame)
        {
            if (frame.Kind != ClrStackFrameType.ManagedMethod ||
                frame.Method.ILOffsetMap == null)
            {
                return -1;
            }

            ulong ip = frame.InstructionPointer;
            int last = -1;

            foreach (ILToNativeMap item in frame.Method.ILOffsetMap)
            {
                if (item.StartAddress > ip)
                {
                    return last;
                }

                if (ip <= item.EndAddress)
                {
                    return item.ILOffset;
                }

                last = item.ILOffset;
            }

            return last;
        }

        private bool IsPortablePdb(Stream stream)
        {
            byte[] buffer = new byte[4];
            stream.Read(buffer, 0, 4);
            stream.Seek(0, SeekOrigin.Begin);
            return buffer[0] == 'B' &&
                buffer[1] == 'S' &&
                buffer[2] == 'J' &&
                buffer[3] == 'B';
        }

        private ISymUnmanagedReader GetReaderForFrame(ClrStackFrame frame)
        {
            ClrModule module = frame.Method?.Type?.Module;
            PdbInfo info = module?.Pdb;
            ISymUnmanagedReader reader = null;
            if (info != null)
            {
                if (_pdbReaders.TryGetValue(info, out reader))
                {
                    return reader;
                }

                if (!File.Exists(info.FileName))
                {
                    return null;
                }

                try
                {
                    Stream stream = File.OpenRead(info.FileName);
                    if (IsPortablePdb(stream))
                    {
                        var bindar = new SymBinder();
                        int result = bindar.GetReaderFromPdbFile(new MetaDataImportProvider(module.MetadataImport), info.FileName, out reader);
                    }
                    else
                    {
                        reader = SymUnmanagedReaderFactory.CreateReaderWithMetadataImport<ISymUnmanagedReader3>(stream, module.MetadataImport, SymUnmanagedReaderCreationOptions.UseComRegistry);
                    }
                }
                catch (Exception e)
                {
                    _logger?.LogDebug(e, "Unable to obtain symbol reader for {0}", info.FileName);
                }
            }

            if (reader != null)
            {
                _pdbReaders.Add(info, reader);
            }

            return reader;
        }
    }
}
