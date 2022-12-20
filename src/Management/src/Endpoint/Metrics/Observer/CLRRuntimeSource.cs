// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.Metrics.Observer;

public static class ClrRuntimeSource
{
    public const string DiagnosticName = "Steeltoe.ClrMetrics";
    public const string HeapEvent = "Steeltoe.ClrMetrics.Heap";
    public const string ThreadsEvent = "Steeltoe.ClrMetrics.Threads";

    public static HeapMetrics GetHeapMetrics()
    {
        long totalMemory = GC.GetTotalMemory(false);

        var counts = new List<long>(GC.MaxGeneration);

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

    public record struct HeapMetrics(long TotalMemory, IList<long> CollectionCounts)
    {
        public readonly long TotalMemory = TotalMemory;
        public readonly IList<long> CollectionCounts = CollectionCounts;
    }

    public record struct ThreadMetrics(long AvailableThreadPoolWorkers, long AvailableThreadCompletionPort, long MaxThreadPoolWorkers,
        long MaxThreadCompletionPort)
    {
        public long AvailableThreadPoolWorkers = AvailableThreadPoolWorkers;
        public long AvailableThreadCompletionPort = AvailableThreadCompletionPort;
        public long MaxThreadPoolWorkers = MaxThreadPoolWorkers;
        public long MaxThreadCompletionPort = MaxThreadCompletionPort;
    }
}
