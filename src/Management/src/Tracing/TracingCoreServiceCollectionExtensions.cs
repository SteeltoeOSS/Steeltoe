// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Trace;

namespace Steeltoe.Management.Tracing;

public static class TracingCoreServiceCollectionExtensions
{
    /// <summary>
    /// Configure distributed tracing via OpenTelemetry with HttpClient and ASP.NET Core Instrumentation along with (optionally) Zipkin exporting.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddDistributedTracingAspNetCore(this IServiceCollection services)
    {
        return services.AddDistributedTracingAspNetCore(null);
    }

    /// <summary>
    /// Configure distributed tracing via OpenTelemetry with HttpClient and ASP.NET Core Instrumentation along with (optionally) Zipkin exporting.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <param name="action">
    /// Customize the <see cref="TracerProviderBuilder" />.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddDistributedTracingAspNetCore(this IServiceCollection services, Action<TracerProviderBuilder>? action)
    {
        ArgumentNullException.ThrowIfNull(services);

        action += builder => builder.AddAspNetCoreInstrumentation();

        services.AddOptions<AspNetCoreTraceInstrumentationOptions>().PostConfigure<IOptionsMonitor<TracingOptions>>(
            (instrumentationOptions, tracingOptionsMonitor) =>
            {
                TracingOptions tracingOptions = tracingOptionsMonitor.CurrentValue;

                if (tracingOptions.IngressIgnorePattern != null)
                {
                    var pathMatcher = new Regex(tracingOptions.IngressIgnorePattern, RegexOptions.None, TimeSpan.FromSeconds(1));
                    instrumentationOptions.Filter += context => !pathMatcher.IsMatch(context.Request.Path);
                }
            });

        return services.AddDistributedTracing(action);
    }
}
