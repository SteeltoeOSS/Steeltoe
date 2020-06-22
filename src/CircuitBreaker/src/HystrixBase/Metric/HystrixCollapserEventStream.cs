﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

        private readonly IHystrixCollapserKey _collapserKey;

        private readonly ISubject<HystrixCollapserEvent, HystrixCollapserEvent> _writeOnlyStream;
        private readonly IObservable<HystrixCollapserEvent> _readOnlyStream;

        public static HystrixCollapserEventStream GetInstance(IHystrixCollapserKey collapserKey)
        {
            return Streams.GetOrAddEx(collapserKey.Name, (k) => new HystrixCollapserEventStream(collapserKey));
        }

        internal HystrixCollapserEventStream(IHystrixCollapserKey collapserKey)
        {
            this._collapserKey = collapserKey;
            this._writeOnlyStream = Subject.Synchronize<HystrixCollapserEvent, HystrixCollapserEvent>(new Subject<HystrixCollapserEvent>());
            this._readOnlyStream = _writeOnlyStream.AsObservable();
        }

        public static void Reset()
        {
            Streams.Clear();
        }

        public void Write(HystrixCollapserEvent @event)
        {
            _writeOnlyStream.OnNext(@event);
        }

        public IObservable<HystrixCollapserEvent> Observe()
        {
            return _readOnlyStream;
        }

        public override string ToString()
        {
            return "HystrixCollapserEventStream(" + _collapserKey.Name + ")";
        }
    }
}
