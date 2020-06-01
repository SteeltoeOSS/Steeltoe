// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Util;
using System;
using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric
{
    public class HystrixThreadPoolStartStream : IHystrixEventStream<HystrixCommandExecutionStarted>
    {
        private static readonly ConcurrentDictionary<string, HystrixThreadPoolStartStream> Streams = new ConcurrentDictionary<string, HystrixThreadPoolStartStream>();

        private readonly IHystrixThreadPoolKey threadPoolKey;
        private readonly ISubject<HystrixCommandExecutionStarted, HystrixCommandExecutionStarted> writeOnlySubject;
        private readonly IObservable<HystrixCommandExecutionStarted> readOnlyStream;

        public static HystrixThreadPoolStartStream GetInstance(IHystrixThreadPoolKey threadPoolKey)
        {
            return Streams.GetOrAddEx(threadPoolKey.Name, (k) => new HystrixThreadPoolStartStream(threadPoolKey));
        }

        internal HystrixThreadPoolStartStream(IHystrixThreadPoolKey threadPoolKey)
        {
            this.threadPoolKey = threadPoolKey;
            this.writeOnlySubject = Subject.Synchronize<HystrixCommandExecutionStarted, HystrixCommandExecutionStarted>(new Subject<HystrixCommandExecutionStarted>());
            this.readOnlyStream = writeOnlySubject.AsObservable();
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
            return "HystrixThreadPoolStartStream(" + threadPoolKey.Name + ")";
        }
    }
}
