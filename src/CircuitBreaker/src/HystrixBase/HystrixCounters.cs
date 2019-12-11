﻿// Copyright 2017 the original author or authors.
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
