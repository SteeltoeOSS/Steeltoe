// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;
using Steeltoe.CircuitBreaker.HystrixBase.Util;
using System.Threading.Tasks;

namespace Steeltoe.CircuitBreaker.Hystrix;

public class HystrixRequestContextMiddleware
{
    private readonly RequestDelegate _next;

#pragma warning disable CS0618 // Type or member is obsolete
    public HystrixRequestContextMiddleware(RequestDelegate next, IApplicationLifetime applicationLifetime)
#pragma warning restore CS0618 // Type or member is obsolete
    {
        _next = next;
        applicationLifetime.ApplicationStopping.Register(HystrixShutdown.ShutdownThreads);
    }

    public async Task Invoke(HttpContext context)
    {
        var hystrix = HystrixRequestContext.InitializeContext();

        await _next.Invoke(context).ConfigureAwait(false);

        hystrix.Dispose();
    }
}
