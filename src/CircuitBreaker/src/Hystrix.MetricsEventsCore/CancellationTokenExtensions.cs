// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsEvents;

public static class CancellationTokenExtensions
{
    public static CancellationTokenAwaiter GetAwaiter(this CancellationToken cancellationToken)
    {
        return new CancellationTokenAwaiter(cancellationToken);
    }

    public class CancellationTokenAwaiter : INotifyCompletion
    {
        private readonly CancellationToken _cancellationToken;

        public CancellationTokenAwaiter(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
        }

        public void GetResult()
        {
            // for future use
        }

        public bool IsCompleted
        {
            get
            {
                return _cancellationToken.IsCancellationRequested;
            }
        }

        public void OnCompleted(Action continuation)
        {
            _cancellationToken.Register(continuation);
        }
    }
}
