// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Xunit;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsEvents.Test;

public class CancellationTokenExtensionsTest
{
    [Fact]
    public void GetAwaiter_WithCancel_IsCompleted()
    {
        var tokenSource = new CancellationTokenSource();
        var awaiter = CancellationTokenExtensions.GetAwaiter(tokenSource.Token);
        Assert.False(awaiter.IsCompleted);
        tokenSource.Cancel();
        Assert.True(awaiter.IsCompleted);
    }
}