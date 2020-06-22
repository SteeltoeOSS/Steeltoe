﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;

namespace Steeltoe.CircuitBreaker.Hystrix
{
    public static class HystrixApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseHystrixMetricsStream(this IApplicationBuilder app)
        {
            return app;
        }
    }
}
