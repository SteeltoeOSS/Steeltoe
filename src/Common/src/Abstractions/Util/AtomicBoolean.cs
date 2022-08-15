// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Util;

public class AtomicBoolean
{
    private volatile int _value;

    public bool Value
    {
        get => _value != 0;
        set => _value = value ? 1 : 0;
    }

    public AtomicBoolean()
        : this(false)
    {
    }

    public AtomicBoolean(bool value)
    {
        Value = value;
    }

    public bool CompareAndSet(bool expected, bool update)
    {
        int expectedInt = expected ? 1 : 0;
        int updateInt = update ? 1 : 0;
        return Interlocked.CompareExchange(ref _value, updateInt, expectedInt) == expectedInt;
    }

    public bool GetAndSet(bool newValue)
    {
        int newValueInt = newValue ? 1 : 0;
        int previous = Interlocked.Exchange(ref _value, newValueInt);
        return previous == 1;
    }
}
