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
