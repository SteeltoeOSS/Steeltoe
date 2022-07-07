// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;

namespace Steeltoe.CircuitBreaker.Hystrix.Util;

public class LongAdder
{
    private readonly AtomicLong _value = new (0);

    public void Increment()
    {
        Add(1);
    }

    public void Decrement()
    {
        Add(-1);
    }

    public void Add(long value)
    {
        _value.AddAndGet(value);
    }

    public long Sum()
    {
        return _value.Value;
    }

    public void Reset()
    {
        _value.GetAndSet(0);
    }

    public long SumThenReset()
    {
        return _value.GetAndSet(0);
    }

    public override string ToString()
    {
        return _value.Value.ToString();
    }
}
