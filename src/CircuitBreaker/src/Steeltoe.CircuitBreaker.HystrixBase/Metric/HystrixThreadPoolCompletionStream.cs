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
using System;
using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric
{
    public class HystrixThreadPoolCompletionStream : IHystrixEventStream<HystrixCommandCompletion>
    {
        private static readonly ConcurrentDictionary<string, HystrixThreadPoolCompletionStream> Streams = new ConcurrentDictionary<string, HystrixThreadPoolCompletionStream>();

        private readonly IHystrixThreadPoolKey threadPoolKey;
        private readonly ISubject<HystrixCommandCompletion, HystrixCommandCompletion> writeOnlySubject;
        private readonly IObservable<HystrixCommandCompletion> readOnlyStream;

        public static HystrixThreadPoolCompletionStream GetInstance(IHystrixThreadPoolKey threadPoolKey)
        {
            return Streams.GetOrAddEx(threadPoolKey.Name, (k) => new HystrixThreadPoolCompletionStream(threadPoolKey));
        }

        internal HystrixThreadPoolCompletionStream(IHystrixThreadPoolKey threadPoolKey)
        {
            this.threadPoolKey = threadPoolKey;
            this.writeOnlySubject = Subject.Synchronize<HystrixCommandCompletion, HystrixCommandCompletion>(new Subject<HystrixCommandCompletion>());
            this.readOnlyStream = writeOnlySubject.AsObservable();
        }

        public static void Reset()
        {
            Streams.Clear();
        }

        public void Write(HystrixCommandCompletion @event)
        {
            writeOnlySubject.OnNext(@event);
        }

        public IObservable<HystrixCommandCompletion> Observe()
        {
            return readOnlyStream;
        }

        public override string ToString()
        {
            return "HystrixThreadPoolCompletionStream(" + threadPoolKey.Name + ")";
        }
    }
}
