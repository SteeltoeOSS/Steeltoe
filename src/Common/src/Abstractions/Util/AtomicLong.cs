// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Util;

public class AtomicLong
{
    private long _value;

    public long Value
    {
        get => Interlocked.Read(ref _value);

        set => Interlocked.Exchange(ref _value, value);
    }

    public AtomicLong()
        : this(0)
    {
    }

    public AtomicLong(long value)
    {
        _value = value;
    }

    public bool CompareAndSet(long expected, long update)
    {
        return Interlocked.CompareExchange(ref _value, update, expected) == expected;
    }

    public long GetAndSet(long value)
    {
        return Interlocked.Exchange(ref _value, value);
    }

    public long AddAndGet(long value)
    {
        return Interlocked.Add(ref _value, value);
    }
}
