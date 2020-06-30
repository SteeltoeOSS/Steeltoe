// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Steeltoe.Management.Endpoint.Metrics.Observer
{
    // Note: This class to be removed when xplat in-process CLR events are supported.
    [Obsolete("Use EventListeners instead")]
    public class CLRRuntimeSource : IPolledDiagnosticSource
    {
        public struct HeapMetrics
        {
            public HeapMetrics(long total, IList<long> collections)
            {
                TotalMemory = total;
                CollectionCounts = collections;
            }

            public long TotalMemory;
            public IList<long> CollectionCounts;
        }

        public struct ThreadMetrics
        {
            public ThreadMetrics(long availWorkers, long availComp, long maxWorkers, long maxComp)
            {
                AvailableThreadPoolWorkers = availWorkers;
                AvailableThreadCompletionPort = availComp;
                MaxThreadPoolWorkers = maxWorkers;
                MaxThreadCompletionPort = maxComp;
            }

            public long AvailableThreadPoolWorkers;
            public long AvailableThreadCompletionPort;
            public long MaxThreadPoolWorkers;
            public long MaxThreadCompletionPort;
        }

        public const string DIAGNOSTIC_NAME = "Steeltoe.ClrMetrics";
        public const string HEAP_EVENT = "Steeltoe.ClrMetrics.Heap";
        public const string THREADS_EVENT = "Steeltoe.ClrMetrics.Threads";

        protected internal DiagnosticSource Source { get; }

        public CLRRuntimeSource()
        {
            Source = new DiagnosticListener(DIAGNOSTIC_NAME);
        }

        public void Poll()
        {
            if (Source.IsEnabled(HEAP_EVENT))
            {
                long totalMemory = GC.GetTotalMemory(false);

                List<long> counts = new List<long>(GC.MaxGeneration);
                for (int i = 0; i < GC.MaxGeneration; i++)
                {
                    counts.Add(GC.CollectionCount(i));
                }

                Source.Write(HEAP_EVENT, new HeapMetrics(totalMemory, counts));
            }

            if (Source.IsEnabled(THREADS_EVENT))
            {
                ThreadPool.GetAvailableThreads(out int availWorkerThreads, out int availCompPortThreads);
                ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxCompPortThreads);
                Source.Write(THREADS_EVENT, new ThreadMetrics(availWorkerThreads, availCompPortThreads, maxWorkerThreads, maxCompPortThreads));
            }
        }
    }
}