// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;

namespace Steeltoe.CircuitBreaker.Hystrix.Util.Test;

internal sealed class MockedTime : ITime
{
    private readonly AtomicInteger _time = new(0);

    public long CurrentTimeInMillis => _time.Value;

    public void Increment(int millis)
    {
        _time.AddAndGet(millis);
    }
}
