// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if NET6_0_OR_GREATER
using System;
using Microsoft.AspNetCore.Builder;
using OpenTelemetry.Trace;

namespace Steeltoe.Management.Tracing;

public static partial class TracingHostBuilderExtensions
{
    /// <summary>
    /// Configure distributed tracing via OpenTelemetry with HttpClient and ASP.NET Core Instrumentation along with (optionally) Zipkin and Wavefront exporting.
    /// </summary>
    /// <param name="hostBuilder">Your HostBuilder.</param>
    /// <param name="action">Customize the <see cref="TracerProviderBuilder" />.</param>
    public static WebApplicationBuilder AddDistributedTracingAspNetCore(this WebApplicationBuilder hostBuilder,  Action<TracerProviderBuilder> action = null)
    {
        hostBuilder.Services.AddDistributedTracingAspNetCore(action);
        return hostBuilder;
    }
}
#endif
