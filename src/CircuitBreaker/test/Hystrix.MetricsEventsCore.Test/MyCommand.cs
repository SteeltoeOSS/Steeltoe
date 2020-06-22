﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsEvents.Test
{
    internal class MyCommand : HystrixCommand<int>
    {
        public MyCommand()
            : base(
                HystrixCommandGroupKeyDefault.AsKey("MyCommandGroup"),
                () => { return 1; },
                () => { return 2; })
        {
        }
    }
}
