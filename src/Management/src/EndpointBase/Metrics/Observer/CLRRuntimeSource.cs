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
                var totalMemory = GC.GetTotalMemory(false);

                var counts = new List<long>(GC.MaxGeneration);
                for (var i = 0; i < GC.MaxGeneration; i++)
                {
                    counts.Add(GC.CollectionCount(i));
                }

                Source.Write(HEAP_EVENT, new HeapMetrics(totalMemory, counts));
            }

            if (Source.IsEnabled(THREADS_EVENT))
            {
                ThreadPool.GetAvailableThreads(out var availWorkerThreads, out var availCompPortThreads);
                ThreadPool.GetMaxThreads(out var maxWorkerThreads, out var maxCompPortThreads);
                Source.Write(THREADS_EVENT, new ThreadMetrics(availWorkerThreads, availCompPortThreads, maxWorkerThreads, maxCompPortThreads));
            }
        }
    }
}