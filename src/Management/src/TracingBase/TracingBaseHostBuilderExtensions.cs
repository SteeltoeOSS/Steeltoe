// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Hosting;
using OpenTelemetry.Trace;
using System;

namespace Steeltoe.Management.Tracing
{
    public static class TracingBaseHostBuilderExtensions
    {
        /// <summary>
        /// Configure distributed tracing via OpenTelemetry with HttpClient Instrumentation.
        /// </summary>
        /// <param name="hostBuilder">Your hostBuilder</param>
        /// <param name="action">Customize the <see cref="TracerProviderBuilder" />.</param>
        /// <returns>The configured hostBuilder</returns>
        public static IHostBuilder AddDistributedTracing(this IHostBuilder hostBuilder, Action<TracerProviderBuilder> action = null)
         => hostBuilder.ConfigureServices((context, services) => services.AddDistributedTracing(action));
    }
}
