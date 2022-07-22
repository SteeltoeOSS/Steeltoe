// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace Steeltoe.CircuitBreaker.Hystrix.Util;

public static class SemaphoreSlimExtensions
{
    public static bool TryAcquire(this SemaphoreSlim sema)
    {
        if (sema == null)
        {
            return true;
        }

        return sema.Wait(0);
    }
}