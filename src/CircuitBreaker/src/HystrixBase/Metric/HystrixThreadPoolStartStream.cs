// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;
using System;
using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric
{
    public class HystrixThreadPoolStartStream : IHystrixEventStream<HystrixCommandExecutionStarted>
    {
        private static readonly ConcurrentDictionary<string, HystrixThreadPoolStartStream> Streams = new ConcurrentDictionary<string, HystrixThreadPoolStartStream>();

        private readonly IHystrixThreadPoolKey _threadPoolKey;
        private readonly ISubject<HystrixCommandExecutionStarted, HystrixCommandExecutionStarted> _writeOnlySubject;
        private readonly IObservable<HystrixCommandExecutionStarted> _readOnlyStream;

        public static HystrixThreadPoolStartStream GetInstance(IHystrixThreadPoolKey threadPoolKey)
        {
            return Streams.GetOrAddEx(threadPoolKey.Name, (k) => new HystrixThreadPoolStartStream(threadPoolKey));
        }

        internal HystrixThreadPoolStartStream(IHystrixThreadPoolKey threadPoolKey)
        {
            _threadPoolKey = threadPoolKey;
            _writeOnlySubject = Subject.Synchronize<HystrixCommandExecutionStarted, HystrixCommandExecutionStarted>(new Subject<HystrixCommandExecutionStarted>());
            _readOnlyStream = _writeOnlySubject.AsObservable();
        }

        public static void Reset()
        {
            Streams.Clear();
        }

        public void Write(HystrixCommandExecutionStarted @event)
        {
            _writeOnlySubject.OnNext(@event);
        }

        public IObservable<HystrixCommandExecutionStarted> Observe()
        {
            return _readOnlyStream;
        }

        public override string ToString()
        {
            return "HystrixThreadPoolStartStream(" + _threadPoolKey.Name + ")";
        }
    }
}
