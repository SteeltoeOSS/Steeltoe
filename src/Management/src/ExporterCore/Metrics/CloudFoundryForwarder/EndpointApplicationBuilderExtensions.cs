// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace Steeltoe.Management.Exporter.Metrics
{
    public static class EndpointApplicationBuilderExtensions
    {
        /// <summary>
        /// Enable the Metric exporter
        /// </summary>
        /// <param name="builder">Your application builder</param>
        public static void UseMetricsExporter(this IApplicationBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var service = builder.ApplicationServices.GetRequiredService<IMetricsExporter>();
#if NETCOREAPP3_0
            var lifetime = builder.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
#else
            var lifetime = builder.ApplicationServices.GetRequiredService<IApplicationLifetime>();
#endif
            lifetime.ApplicationStopping.Register(() => service.Stop());
            service.Start();
        }
    }
}
