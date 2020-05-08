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

using Steeltoe.Common;
using System;
using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric
{
    public class HystrixCommandStartStream : IHystrixEventStream<HystrixCommandExecutionStarted>
    {
        private static readonly ConcurrentDictionary<string, HystrixCommandStartStream> Streams = new ConcurrentDictionary<string, HystrixCommandStartStream>();

        private readonly IHystrixCommandKey commandKey;
        private readonly ISubject<HystrixCommandExecutionStarted, HystrixCommandExecutionStarted> writeOnlySubject;
        private readonly IObservable<HystrixCommandExecutionStarted> readOnlyStream;

        public static HystrixCommandStartStream GetInstance(IHystrixCommandKey commandKey)
        {
            return Streams.GetOrAddEx(commandKey.Name, (k) => new HystrixCommandStartStream(commandKey));
        }

        internal HystrixCommandStartStream(IHystrixCommandKey commandKey)
        {
            this.commandKey = commandKey;
            writeOnlySubject = Subject.Synchronize<HystrixCommandExecutionStarted, HystrixCommandExecutionStarted>(new Subject<HystrixCommandExecutionStarted>());
            readOnlyStream = writeOnlySubject.AsObservable();
        }

        public static void Reset()
        {
            Streams.Clear();
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
            return "HystrixCommandStartStream(" + commandKey.Name + ")";
        }
    }
}
