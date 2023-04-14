// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Trace;
using Steeltoe.Common;

namespace Steeltoe.Management.Tracing;

public static class TracingBaseHostBuilderExtensions
{
    /// <summary>
    /// Configure distributed tracing via OpenTelemetry with HttpClient Instrumentation.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your hostBuilder.
    /// </param>
    /// <param name="action">
    /// Customize the <see cref="TracerProviderBuilder" />.
    /// </param>
    /// <returns>
    /// The configured hostBuilder.
    /// </returns>
    public static IHostBuilder AddDistributedTracing(this IHostBuilder hostBuilder, Action<TracerProviderBuilder> action)
    {
        ArgumentGuard.NotNull(hostBuilder);
        return hostBuilder.ConfigureServices((_, services) => services.AddDistributedTracing(action));
    }
}
