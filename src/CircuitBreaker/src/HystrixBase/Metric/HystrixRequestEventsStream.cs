// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric
{
    public class HystrixRequestEventsStream
    {
        private readonly ISubject<HystrixRequestEvents, HystrixRequestEvents> _writeOnlyRequestEventsSubject;
        private readonly IObservable<HystrixRequestEvents> _readOnlyRequestEvents;

        internal HystrixRequestEventsStream()
        {
            this._writeOnlyRequestEventsSubject = Subject.Synchronize<HystrixRequestEvents, HystrixRequestEvents>(new Subject<HystrixRequestEvents>());
            this._readOnlyRequestEvents = _writeOnlyRequestEventsSubject.AsObservable();
        }

        private static readonly HystrixRequestEventsStream INSTANCE = new HystrixRequestEventsStream();

        public static HystrixRequestEventsStream GetInstance()
        {
            return INSTANCE;
        }

        public void Shutdown()
        {
            _writeOnlyRequestEventsSubject.OnCompleted();
        }

        public void Write(ICollection<IHystrixInvokableInfo> executions)
        {
            HystrixRequestEvents requestEvents = new HystrixRequestEvents(executions);
            _writeOnlyRequestEventsSubject.OnNext(requestEvents);
        }

        public IObservable<HystrixRequestEvents> Observe()
        {
            return _readOnlyRequestEvents;
        }
    }
}
