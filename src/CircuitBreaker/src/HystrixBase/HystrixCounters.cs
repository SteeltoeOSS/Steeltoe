// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;

namespace Steeltoe.CircuitBreaker.Hystrix
{
    public static class HystrixCounters
    {
        private static readonly AtomicInteger _concurrentThreadsExecuting = new AtomicInteger(0);

        internal static int IncrementGlobalConcurrentThreads()
        {
            return _concurrentThreadsExecuting.IncrementAndGet();
        }

        internal static int DecrementGlobalConcurrentThreads()
        {
            return _concurrentThreadsExecuting.DecrementAndGet();
        }

        public static int GlobalConcurrentThreadsExecuting => _concurrentThreadsExecuting.Value;

        public static int CommandCount => HystrixCommandKeyDefault.CommandCount;

        public static int ThreadPoolCount => HystrixThreadPoolKeyDefault.ThreadPoolCount;

        public static int GroupCount => HystrixCommandGroupKeyDefault.GroupCount;
    }
}
