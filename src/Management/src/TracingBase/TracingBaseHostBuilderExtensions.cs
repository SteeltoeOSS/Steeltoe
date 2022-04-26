// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Hosting;

namespace Steeltoe.Management.Tracing
{
    public static class TracingBaseHostBuilderExtensions
    {
        /// <summary>
        /// Configure distributed tracing via OpenTelemetry with HttpClient Instrumentation.
        /// </summary>
        /// <param name="hostBuilder">Your hostBuilder</param>
        /// <returns>The configured hostBuilder</returns>
        public static IHostBuilder AddDistributedTracing(this IHostBuilder hostBuilder)
         => hostBuilder.ConfigureServices((context, services) => services.AddDistributedTracing());
    }
}
