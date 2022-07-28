// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

internal sealed class BasicDelayCommand : HystrixCommand<int>
{
    public int Delay { get; }

    public bool ThrowException { get; }

    public BasicDelayCommand(int delay, bool throwException)
        : base(HystrixCommandGroupKeyDefault.AsKey("BasicDelayCommand"), delay * 3)
    {
        Delay = delay;
        ThrowException = throwException;
    }

    protected override int Run()
    {
        Thread.Sleep(Delay);
        return Delay;
    }

    protected override int RunFallback()
    {
        if (ThrowException)
        {
            throw new Exception("RunFallback Exception");
        }

        return Delay;
    }
}
