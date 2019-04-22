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
    public class HystrixCollapserEventStream : IHystrixEventStream<HystrixCollapserEvent>
    {
        private static readonly ConcurrentDictionary<string, HystrixCollapserEventStream> Streams = new ConcurrentDictionary<string, HystrixCollapserEventStream>();

        private readonly IHystrixCollapserKey collapserKey;

        private readonly ISubject<HystrixCollapserEvent, HystrixCollapserEvent> writeOnlyStream;
        private readonly IObservable<HystrixCollapserEvent> readOnlyStream;

        public static HystrixCollapserEventStream GetInstance(IHystrixCollapserKey collapserKey)
        {
            return Streams.GetOrAddEx(collapserKey.Name, (k) => new HystrixCollapserEventStream(collapserKey));
        }

        internal HystrixCollapserEventStream(IHystrixCollapserKey collapserKey)
        {
            this.collapserKey = collapserKey;
            this.writeOnlyStream = Subject.Synchronize<HystrixCollapserEvent, HystrixCollapserEvent>(new Subject<HystrixCollapserEvent>());
            this.readOnlyStream = writeOnlyStream.AsObservable();
        }

        public static void Reset()
        {
            Streams.Clear();
        }

        public void Write(HystrixCollapserEvent @event)
        {
            writeOnlyStream.OnNext(@event);
        }

        public IObservable<HystrixCollapserEvent> Observe()
        {
            return readOnlyStream;
        }

        public override string ToString()
        {
            return "HystrixCollapserEventStream(" + collapserKey.Name + ")";
        }
    }
}
