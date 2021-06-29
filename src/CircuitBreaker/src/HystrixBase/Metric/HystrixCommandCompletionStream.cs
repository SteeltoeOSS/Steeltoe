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
    public class HystrixCommandCompletionStream : IHystrixEventStream<HystrixCommandCompletion>
    {
        private static readonly ConcurrentDictionary<string, HystrixCommandCompletionStream> Streams = new ConcurrentDictionary<string, HystrixCommandCompletionStream>();

        private readonly IHystrixCommandKey _commandKey;
        private readonly ISubject<HystrixCommandCompletion, HystrixCommandCompletion> _writeOnlySubject;
        private readonly IObservable<HystrixCommandCompletion> _readOnlyStream;

        public static HystrixCommandCompletionStream GetInstance(IHystrixCommandKey commandKey)
        {
            return Streams.GetOrAddEx(commandKey.Name, (k) => new HystrixCommandCompletionStream(commandKey));
        }

        internal HystrixCommandCompletionStream(IHystrixCommandKey commandKey)
        {
            _commandKey = commandKey;
            _writeOnlySubject = Subject.Synchronize<HystrixCommandCompletion, HystrixCommandCompletion>(new Subject<HystrixCommandCompletion>());
            _readOnlyStream = _writeOnlySubject.AsObservable();
        }

        public static void Reset()
        {
            Streams.Clear();
        }

        public void Write(HystrixCommandCompletion @event)
        {
            _writeOnlySubject.OnNext(@event);
        }

        public IObservable<HystrixCommandCompletion> Observe()
        {
            return _readOnlyStream;
        }

        public override string ToString()
        {
            return "HystrixCommandCompletionStream(" + _commandKey.Name + ")";
        }
    }
}