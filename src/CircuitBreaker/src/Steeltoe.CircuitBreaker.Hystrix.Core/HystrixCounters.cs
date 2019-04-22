//
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

using Steeltoe.CircuitBreaker.Hystrix.Util;


namespace Steeltoe.CircuitBreaker.Hystrix
{
    public static class HystrixCounters
    {
        private static readonly AtomicInteger concurrentThreadsExecuting = new AtomicInteger(0);

        internal static int IncrementGlobalConcurrentThreads()
        {
            return concurrentThreadsExecuting.IncrementAndGet();
        }

        internal static int DecrementGlobalConcurrentThreads()
        {
            return concurrentThreadsExecuting.DecrementAndGet();
        }


        public static int GlobalConcurrentThreadsExecuting
        {
            get { return concurrentThreadsExecuting.Value; }
        }

  
        public static int CommandCount
        {
            get { return HystrixCommandKeyDefault.CommandCount; }
        }


        public static int ThreadPoolCount 
        {
            get { return HystrixThreadPoolKeyDefault.ThreadPoolCount; }
        }

        public static int GroupCount
        {
            get { return HystrixCommandGroupKeyDefault.GroupCount; }
        }
    }

}
