// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Trace;

namespace Steeltoe.Management.Tracing;

public static class TracingHostBuilderExtensions
{
    /// <summary>
    /// Configure distributed tracing via OpenTelemetry with HttpClient and ASP.NET Core Instrumentation along with (optionally) Zipkin and Wavefront
    /// exporting.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    /// <param name="action">
    /// Customize the <see cref="TracerProviderBuilder" />.
    /// </param>
    public static IHostBuilder AddDistributedTracingAspNetCore(this IHostBuilder hostBuilder, Action<TracerProviderBuilder> action)
    {
        return hostBuilder.ConfigureServices(services => services.AddDistributedTracingAspNetCore(action));
    }

    /// <summary>
    /// Configure distributed tracing via OpenTelemetry with HttpClient and ASP.NET Core Instrumentation along with (optionally) Zipkin and Wavefront
    /// exporting.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    public static IHostBuilder AddDistributedTracingAspNetCore(this IHostBuilder hostBuilder) => hostBuilder.AddDistributedTracingAspNetCore(null);
    /// <summary>
    /// Configure distributed tracing via OpenTelemetry with HttpClient and ASP.NET Core Instrumentation along with (optionally) Zipkin and Wavefront
    /// exporting.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    /// <param name="action">
    /// Customize the <see cref="TracerProviderBuilder" />.
    /// </param>
    public static IWebHostBuilder AddDistributedTracingAspNetCore(this IWebHostBuilder hostBuilder, Action<TracerProviderBuilder> action)
    {
        return hostBuilder.ConfigureServices((_, services) => services.AddDistributedTracingAspNetCore(action));
    }
    /// <summary>
    /// Configure distributed tracing via OpenTelemetry with HttpClient and ASP.NET Core Instrumentation along with (optionally) Zipkin and Wavefront
    /// exporting.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    public static IWebHostBuilder AddDistributedTracingAspNetCore(this IWebHostBuilder hostBuilder) => hostBuilder.AddDistributedTracingAspNetCore(null);

    /// <summary>
    /// Configure distributed tracing via OpenTelemetry with HttpClient and ASP.NET Core Instrumentation along with (optionally) Zipkin and Wavefront
    /// exporting.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    /// <param name="action">
    /// Customize the <see cref="TracerProviderBuilder" />.
    /// </param>
    public static WebApplicationBuilder AddDistributedTracingAspNetCore(this WebApplicationBuilder hostBuilder, Action<TracerProviderBuilder> action)
    {
        hostBuilder.Services.AddDistributedTracingAspNetCore(action);
        return hostBuilder;
    }
    /// <summary>
    /// Configure distributed tracing via OpenTelemetry with HttpClient and ASP.NET Core Instrumentation along with (optionally) Zipkin and Wavefront
    /// exporting.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    public static WebApplicationBuilder AddDistributedTracingAspNetCore(this WebApplicationBuilder hostBuilder) => hostBuilder.AddDistributedTracingAspNetCore(null);
}
