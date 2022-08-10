// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reactive;
using System.Text;
using Steeltoe.Common.Util;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

public class TestObserverBase<T> : ObserverBase<T>
{
    public const int StableTickCount = 2;

    private readonly CountdownEvent _latch;
    private readonly ITestOutputHelper _output;

    public volatile int TickCount;

    public volatile bool StreamRunning;

    public TestObserverBase(ITestOutputHelper output, CountdownEvent latch)
    {
        _latch = latch;
        _output = output;
    }

    protected override void OnCompletedCore()
    {
        _output?.WriteLine("OnComplete @ " + Time.CurrentTimeMillis + " :" + Thread.CurrentThread.ManagedThreadId);
        StreamRunning = false;
        _latch.SignalEx();
    }

    protected override void OnErrorCore(Exception error)
    {
        Assert.False(true, error.Message);
    }

    protected override void OnNextCore(T value)
    {
        TickCount++;

        if (TickCount >= StableTickCount)
        {
            StreamRunning = true;
        }

        if (_output != null)
        {
            string toString = value.ToString();

            if (value is Array array)
            {
                toString = Join(",", array);
            }

            _output.WriteLine("OnNext @ " + Time.CurrentTimeMillis + " :" + Thread.CurrentThread.ManagedThreadId + " : Value= " + toString);
            _output.WriteLine("ReqLog" + "@ " + Time.CurrentTimeMillis + " : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
        }
    }

    private string Join(string v, Array array)
    {
        var sb = new StringBuilder("[");

        foreach (object val in array)
        {
            sb.Append(val);
            sb.Append(v);
        }

        return $"{sb.ToString(0, sb.Length - 1)}]";
    }
}
