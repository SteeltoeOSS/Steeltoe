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
//

using System;
using System.Collections.Concurrent;
using System.Reactive.Subjects;
using System.Reactive.Linq;

using System.Threading.Tasks;
using Steeltoe.CircuitBreaker.Hystrix.Util;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric
{
    public class HystrixThreadPoolStartStream : IHystrixEventStream<HystrixCommandExecutionStarted>
    {

        private readonly IHystrixThreadPoolKey threadPoolKey;

        private readonly ISubject<HystrixCommandExecutionStarted, HystrixCommandExecutionStarted> writeOnlySubject;
        private readonly IObservable<HystrixCommandExecutionStarted> readOnlyStream;

        private static readonly ConcurrentDictionary<string, HystrixThreadPoolStartStream> streams = new ConcurrentDictionary<string, HystrixThreadPoolStartStream>();

        public static HystrixThreadPoolStartStream GetInstance(IHystrixThreadPoolKey threadPoolKey)
        {
            return streams.GetOrAddEx(threadPoolKey.Name, (k) => new HystrixThreadPoolStartStream(threadPoolKey));
        }

        HystrixThreadPoolStartStream(IHystrixThreadPoolKey threadPoolKey)
        {
 
            this.threadPoolKey = threadPoolKey;
            this.writeOnlySubject = Subject.Synchronize<HystrixCommandExecutionStarted, HystrixCommandExecutionStarted>(new Subject<HystrixCommandExecutionStarted>());
            this.readOnlyStream = writeOnlySubject.AsObservable();
        }

        public static void Reset()
        {
            streams.Clear();
        }

        public void Write(HystrixCommandExecutionStarted @event)
        {
            writeOnlySubject.OnNext(@event);
        }


        public IObservable<HystrixCommandExecutionStarted> Observe()
        {
            return readOnlyStream;
        }

        public override string ToString()
        {
            return "HystrixThreadPoolStartStream(" + threadPoolKey.Name + ")";
        }
    }
}
