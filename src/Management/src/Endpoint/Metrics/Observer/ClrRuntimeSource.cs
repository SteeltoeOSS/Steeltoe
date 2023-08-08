// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.Metrics.Observer;

internal static class ClrRuntimeSource
{
    public static HeapMetrics GetHeapMetrics()
    {
        long totalMemory = GC.GetTotalMemory(false);

        var counts = new List<long>(GC.MaxGeneration);

        for (int index = 0; index < GC.MaxGeneration; index++)
        {
            counts.Add(GC.CollectionCount(index));
        }

        return new HeapMetrics(totalMemory, counts);
    }

    public static ThreadMetrics GetThreadMetrics()
    {
        ThreadPool.GetAvailableThreads(out int availWorkerThreads, out int availCompPortThreads);
        ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxCompPortThreads);
        return new ThreadMetrics(availWorkerThreads, availCompPortThreads, maxWorkerThreads, maxCompPortThreads);
    }

    public record struct HeapMetrics(long TotalMemory, IList<long> CollectionCounts)
    {
        public readonly long TotalMemory = TotalMemory;
        public readonly IList<long> CollectionCounts = CollectionCounts;
    }

    public record struct ThreadMetrics(long AvailableThreadPoolWorkers, long AvailableThreadCompletionPort, long MaxThreadPoolWorkers,
        long MaxThreadCompletionPort)
    {
        public readonly long AvailableThreadPoolWorkers = AvailableThreadPoolWorkers;
        public readonly long AvailableThreadCompletionPort = AvailableThreadCompletionPort;
        public readonly long MaxThreadPoolWorkers = MaxThreadPoolWorkers;
        public readonly long MaxThreadCompletionPort = MaxThreadCompletionPort;
    }
}
