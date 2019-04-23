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

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric
{
    public class HystrixRequestEventsStream
    {
        private readonly ISubject<HystrixRequestEvents, HystrixRequestEvents> writeOnlyRequestEventsSubject;
        private readonly IObservable<HystrixRequestEvents> readOnlyRequestEvents;

        internal HystrixRequestEventsStream()
        {
            this.writeOnlyRequestEventsSubject = Subject.Synchronize<HystrixRequestEvents, HystrixRequestEvents>(new Subject<HystrixRequestEvents>());
            this.readOnlyRequestEvents = writeOnlyRequestEventsSubject.AsObservable();
        }

        private static readonly HystrixRequestEventsStream INSTANCE = new HystrixRequestEventsStream();

        public static HystrixRequestEventsStream GetInstance()
        {
            return INSTANCE;
        }

        public void Shutdown()
        {
            writeOnlyRequestEventsSubject.OnCompleted();
        }

        public void Write(ICollection<IHystrixInvokableInfo> executions)
        {
            HystrixRequestEvents requestEvents = new HystrixRequestEvents(executions);
            writeOnlyRequestEventsSubject.OnNext(requestEvents);
        }

        public IObservable<HystrixRequestEvents> Observe()
        {
            return readOnlyRequestEvents;
        }
    }
}
