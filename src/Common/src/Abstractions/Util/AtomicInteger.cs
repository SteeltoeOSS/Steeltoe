// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Util;

public class AtomicInteger
{
    protected volatile int value;

    public int Value
    {
        get => value;

        set => this.value = value;
    }

    public AtomicInteger()
        : this(0)
    {
    }

    public AtomicInteger(int value)
    {
        this.value = value;
    }

    public bool CompareAndSet(int expected, int update)
    {
        return Interlocked.CompareExchange(ref value, update, expected) == expected;
    }

    public int IncrementAndGet()
    {
        return Interlocked.Increment(ref value);
    }

    public int DecrementAndGet()
    {
        return Interlocked.Decrement(ref value);
    }

    public int GetAndIncrement()
    {
        return Interlocked.Increment(ref value) - 1;
    }

    public int AddAndGet(int value)
    {
        return Interlocked.Add(ref this.value, value);
    }
}
