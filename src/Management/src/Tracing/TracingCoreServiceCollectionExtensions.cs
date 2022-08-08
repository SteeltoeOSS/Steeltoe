// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Trace;
using Steeltoe.Common;
using Steeltoe.Management.OpenTelemetry.Trace;

namespace Steeltoe.Management.Tracing;

public static class TracingCoreServiceCollectionExtensions
{
    /// <summary>
    /// Configure distributed tracing via OpenTelemetry with HttpClient and ASP.NET Core Instrumentation along with (optionally) Zipkin exporting.
    /// </summary>
    /// <param name="services">
    /// <see cref="IServiceCollection" />.
    /// </param>
    /// <returns>
    /// <see cref="IServiceCollection" /> configured for distributed tracing.
    /// </returns>
    public static IServiceCollection AddDistributedTracingAspNetCore(this IServiceCollection services)
    {
        return services.AddDistributedTracingAspNetCore(null);
    }

    /// <summary>
    /// Configure distributed tracing via OpenTelemetry with HttpClient and ASP.NET Core Instrumentation along with (optionally) Zipkin exporting.
    /// </summary>
    /// <param name="services">
    /// <see cref="IServiceCollection" />.
    /// </param>
    /// <param name="action">
    /// Customize the <see cref="TracerProviderBuilder" />.
    /// </param>
    /// <returns>
    /// <see cref="IServiceCollection" /> configured for distributed tracing.
    /// </returns>
    public static IServiceCollection AddDistributedTracingAspNetCore(this IServiceCollection services, Action<TracerProviderBuilder> action)
    {
        ArgumentGuard.NotNull(services);

        action += builder => builder.AddAspNetCoreInstrumentation();

        services.AddOptions<AspNetCoreInstrumentationOptions>().PostConfigure<ITracingOptions>((options, traceOpts) =>
        {
            var pathMatcher = new Regex(traceOpts.IngressIgnorePattern);
            options.EnableGrpcAspNetCoreSupport = traceOpts.EnableGrpcAspNetCoreSupport;
            options.Filter += context => !pathMatcher.IsMatch(context.Request.Path);
        });

        return services.AddDistributedTracing(action);
    }
}
