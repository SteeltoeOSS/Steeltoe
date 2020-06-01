// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Util;

namespace Steeltoe.CircuitBreaker.Hystrix.Collapser
{
    public interface ICollapserTimer
    {
        TimerReference AddListener(ITimerListener collapseTask);
    }
}
