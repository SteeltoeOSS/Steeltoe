// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
        private const int PdbHiddenLine = 0xFEEFEE;
        private readonly ILogger<ThreadDumper> _logger;
        private readonly IThreadDumpOptions _options;
        private readonly Dictionary<PdbInfo, ISymUnmanagedReader> _pdbReaders = new Dictionary<PdbInfo, ISymUnmanagedReader>();

        public ThreadDumper(IThreadDumpOptions options, ILogger<ThreadDumper> logger = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
        }

        public List<ThreadInfo> DumpThreads()
        {
            var results = new List<ThreadInfo>();

            var dataTarget = CreateDataTarget();
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
                    var totalMemory = GC.GetTotalMemory(true);
                    _logger?.LogDebug("Total Memory {0}", totalMemory);
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
                var allLocks = GetAllLocks(runtime);

                _logger?.LogTrace("Starting thread walk");

                foreach (var thread in runtime.Threads)
                {
                    if (thread.IsAlive)
                    {
                        var threadInfo = new ThreadInfo()
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
                            LockInfo lockInfo;
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
            var result = new List<LockInfo>();
            foreach (var lck in allLocks)
            {
                if (lck.Reason != BlockingReason.Monitor && lck.Reason != BlockingReason.MonitorWait && thread.Address == lck.Owner?.Address)
                {
                    var lockClass = thread.Runtime.Heap.GetObjectType(lck.Object);
                    if (lockClass != null)
                    {
                        var info = new LockInfo()
                        {
                            ClassName = lockClass.Name,
                            IdentityHashCode = (int)lck.Object
                        };
                        result.Add(info);
                    }
                }
            }

            _logger?.LogTrace("Thread has {0} non monitor locks", result.Count);
            return result;
        }

        private List<MonitorInfo> GetThisThreadsLockedMonitors(ClrThread thread, IList<BlockingObject> allLocks)
        {
            var result = new List<MonitorInfo>();

            foreach (var lck in allLocks)
            {
                if ((lck.Reason == BlockingReason.Monitor || lck.Reason == BlockingReason.MonitorWait) && thread.Address == lck.Owner?.Address)
                {
                    var lockClass = thread.Runtime.Heap.GetObjectType(lck.Object);
                    if (lockClass != null)
                    {
                        var info = new MonitorInfo()
                        {
                            ClassName = lockClass.Name,
                            IdentityHashCode = (int)lck.Object
                        };
                        result.Add(info);
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
            var result = false;
            if (elements.Count > 0)
            {
                result = elements[0].IsNativeMethod;
            }

            _logger?.LogTrace("Thread in native method {0}", result);
            return result;
        }

        private List<StackTraceElement> GetStackTrace(ClrThread thread)
        {
            var result = new List<StackTraceElement>();

            _logger?.LogTrace("Starting stack dump for thread");

            if (thread.IsAlive)
            {
                var stackTrace = thread.StackTrace;
                if (stackTrace != null)
                {
                    foreach (var frame in stackTrace)
                    {
                        var element = GetStackTraceElement(frame);
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

            var info = GetSourceLocation(frame);
            var result = new StackTraceElement()
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
            var result = TState.NEW;
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
            var result = false;
            if (thread.BlockingObjects != null)
            {
                foreach (var lck in thread.BlockingObjects)
                {
                    if (lck.Taken && (lck.Reason == BlockingReason.Monitor || lck.Reason == BlockingReason.MonitorWait))
                    {
                        result = true;
                    }
                }
            }

            return result;
        }

        private bool IsWaitingOnNonMonitor(ClrThread thread)
        {
            var result = false;
            if (thread.BlockingObjects != null)
            {
                foreach (var lck in thread.BlockingObjects)
                {
                    if (lck.Taken && lck.Reason != BlockingReason.Monitor && lck.Reason != BlockingReason.MonitorWait)
                    {
                        result = true;
                    }
                }
            }

            return result;
        }

        private string GetThreadName(ClrThread thread)
        {
            var id = thread.OSThreadId;
            var name = "Thread-" + id;

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
            var locks = new List<BlockingObject>();

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
                _ = reader.GetMethod((int)method.MetadataToken, out var methodSym);
                if (methodSym != null)
                {
                    var seqPoints = methodSym.GetSequencePoints();
                    var ilOffset = FindIlOffset(frame);
                    if (ilOffset >= 0)
                    {
                        var nearest = FindNearestLine(seqPoints, ilOffset);
                        if (nearest.Line == PdbHiddenLine)
                        {
                            nearest.Line = 0;
                        }

                        _logger?.LogTrace("FindNearestLine for {0} in method {1} returning {2} in {3}", ilOffset, method.Name, nearest.Line, nearest.File);
                        return nearest;
                    }
                }
            }

            return default;
        }

        private FileAndLineNumber FindNearestLine(IEnumerable<SymUnmanagedSequencePoint> sequences, int ilOffset)
        {
            var distance = int.MaxValue;
            var nearest = default(FileAndLineNumber);

            foreach (var point in sequences)
            {
                var dist = Math.Abs(point.Offset - ilOffset);
                if (dist < distance)
                {
                    nearest.File = Path.GetFileName(point.Document.GetName());
                    nearest.Line = point.StartLine;
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

            var ip = frame.InstructionPointer;
            var last = -1;

            foreach (var item in frame.Method.ILOffsetMap)
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
            var buffer = new byte[4];
            stream.Read(buffer, 0, 4);
            stream.Seek(0, SeekOrigin.Begin);
            return buffer[0] == 'B' &&
                buffer[1] == 'S' &&
                buffer[2] == 'J' &&
                buffer[3] == 'B';
        }

        private ISymUnmanagedReader GetReaderForFrame(ClrStackFrame frame)
        {
            var module = frame.Method?.Type?.Module;
            var info = module?.Pdb;
            ISymUnmanagedReader reader = null;
            var name = string.Empty;

            if (info != null)
            {
                if (_pdbReaders.TryGetValue(info, out reader))
                {
                    return reader;
                }

                name = Path.GetFileName(info.FileName);
                if (!File.Exists(name))
                {
                    _logger?.LogTrace("Symbol file {0} missing", name);
                    return null;
                }

                try
                {
                    Stream stream = File.OpenRead(name);
                    if (IsPortablePdb(stream))
                    {
                        var bindar = new SymBinder();
                        var result = bindar.GetReaderFromPdbFile(new MetaDataImportProvider(module.MetadataImport), name, out reader);
                    }
                    else
                    {
                        reader = SymUnmanagedReaderFactory.CreateReaderWithMetadataImport<ISymUnmanagedReader3>(stream, module.MetadataImport, SymUnmanagedReaderCreationOptions.Default);
                    }
                }
                catch (Exception e)
                {
                    _logger?.LogError(e, "Unable to obtain symbol reader for {0}", name);
                }
            }

            if (reader != null)
            {
                _logger?.LogTrace("Symbol file {0} found, reader created", name);
                _pdbReaders.Add(info, reader);
            }
            else
            {
                _logger?.LogTrace("Unable to obtain symbol reader for {0}", name);
            }

            return reader;
        }
    }
}
