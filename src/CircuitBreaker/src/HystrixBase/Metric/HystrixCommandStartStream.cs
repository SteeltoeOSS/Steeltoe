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
            return "HystrixCommandStartStream(" + commandKey.Name + ")";
        }
    }
}
