// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

public static class CountDownEventExtensions
{
    public static void SignalEx(this CountdownEvent target)
    {
        try
        {
            target.Signal();
        }
        catch (InvalidOperationException)
        {
            // Intentionally left empty.
        }
    }
}
