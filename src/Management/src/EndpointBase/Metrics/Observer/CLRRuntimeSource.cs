// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;

namespace Steeltoe.Management.Endpoint.Metrics.Observer;

public static class ClrRuntimeSource
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

    public const string DiagnosticName = "Steeltoe.ClrMetrics";
    public const string HeapEvent = "Steeltoe.ClrMetrics.Heap";
    public const string ThreadsEvent = "Steeltoe.ClrMetrics.Threads";

    public static HeapMetrics GetHeapMetrics()
    {
        long totalMemory = GC.GetTotalMemory(false);

        List<long> counts = new List<long>(GC.MaxGeneration);
        for (int i = 0; i < GC.MaxGeneration; i++)
        {
            counts.Add(GC.CollectionCount(i));
        }

        return new HeapMetrics(totalMemory, counts);
    }

    public static ThreadMetrics GetThreadMetrics()
    {
        ThreadPool.GetAvailableThreads(out int availWorkerThreads, out int availCompPortThreads);
        ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxCompPortThreads);
        return new ThreadMetrics(availWorkerThreads, availCompPortThreads, maxWorkerThreads, maxCompPortThreads);
    }
}
