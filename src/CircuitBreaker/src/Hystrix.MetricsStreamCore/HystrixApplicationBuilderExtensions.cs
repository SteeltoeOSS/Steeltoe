// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.CircuitBreaker.Hystrix.MetricsStream;
using System;

namespace Steeltoe.CircuitBreaker.Hystrix;

public static class HystrixApplicationBuilderExtensions
{
    [Obsolete("This functionality is now performed by an IHostedService, you may remove this method call")]
    public static IApplicationBuilder UseHystrixMetricsStream(this IApplicationBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        _ = builder.ApplicationServices.GetRequiredService<RabbitMetricsStreamPublisher>();
        return builder;
    }
}
