// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Util;
using Steeltoe.Common.Util;

namespace Steeltoe.CircuitBreaker.Hystrix.Collapser;

public class RealCollapserTimer : ICollapserTimer
{
    private static readonly HystrixTimer Timer = HystrixTimer.GetInstance();

    public TimerReference AddListener(ITimerListener collapseTask)
    {
        return Timer.AddTimerListener(collapseTask);
    }
}
