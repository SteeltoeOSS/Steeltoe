// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Steeltoe.Common.Util;

namespace Steeltoe.CircuitBreaker.Hystrix.Util;

public class LongMaxUpdater
{
    private readonly AtomicLong _value = new(long.MinValue);

    public long Max => _value.Value;

    public void Update(long value)
    {
        while (true)
        {
            long current = _value.Value;

            if (current >= value)
            {
                return;
            }

            if (_value.CompareAndSet(current, value))
            {
                return;
            }
        }
    }

    public void Reset()
    {
        _value.GetAndSet(long.MinValue);
    }

    public long MaxThenReset()
    {
        return _value.GetAndSet(long.MinValue);
    }

    public override string ToString()
    {
        return _value.Value.ToString(CultureInfo.InvariantCulture);
    }
}
