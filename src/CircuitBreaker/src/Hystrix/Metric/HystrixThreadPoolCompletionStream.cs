// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Steeltoe.Common;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric;

public class HystrixThreadPoolCompletionStream : IHystrixEventStream<HystrixCommandCompletion>
{
    private static readonly ConcurrentDictionary<string, HystrixThreadPoolCompletionStream> Streams = new();

    private readonly IHystrixThreadPoolKey _threadPoolKey;
    private readonly ISubject<HystrixCommandCompletion, HystrixCommandCompletion> _writeOnlySubject;
    private readonly IObservable<HystrixCommandCompletion> _readOnlyStream;

    internal HystrixThreadPoolCompletionStream(IHystrixThreadPoolKey threadPoolKey)
    {
        _threadPoolKey = threadPoolKey;
        _writeOnlySubject = Subject.Synchronize<HystrixCommandCompletion, HystrixCommandCompletion>(new Subject<HystrixCommandCompletion>());
        _readOnlyStream = _writeOnlySubject.AsObservable();
    }

    public static HystrixThreadPoolCompletionStream GetInstance(IHystrixThreadPoolKey threadPoolKey)
    {
        return Streams.GetOrAddEx(threadPoolKey.Name, _ => new HystrixThreadPoolCompletionStream(threadPoolKey));
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
        return $"HystrixThreadPoolCompletionStream({_threadPoolKey.Name})";
    }
}
