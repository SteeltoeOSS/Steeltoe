// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.CommandLine;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace Steeltoe.Extensions.Configuration.SpringBoot
{
    public static class SpringBootHostBuilderExtensions
    {
        /// <summary>
        ///  Sets up the configuration provider in spring boot style '.' separated values in CommandLine or as SPRING_APPLICATION_JSON Environment variable
        /// </summary>
        /// <param name="builder"><see cref="IHostBuilder"/></param>
        /// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
        public static IHostBuilder AddSpringBootConfiguration(this IHostBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.ConfigureAppConfiguration((c, b) =>
                {
                    b.AddSpringBootEnv();
                    b.AddSpringBootCmd(c.Configuration);
                });
        }

        /// <summary>
        ///  Sets up the configuration provider in spring boot style '.' separated values in CommandLine or as SPRING_APPLICATION_JSON Environment variable
        /// </summary>
        /// <param name="builder"><see cref="IWebHostBuilder"/></param>
        /// <returns>The same instance of the <see cref="IWebHostBuilder"/> for chaining.</returns>
        public static IWebHostBuilder AddSpringBootConfiguration(this IWebHostBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.ConfigureAppConfiguration((c, b) =>
            {
                b.AddSpringBootEnv();
                b.AddSpringBootCmd(c.Configuration);
            });
        }

#if NET6_0_OR_GREATER
        /// <summary>
        ///  Sets up the configuration provider in spring boot style '.' separated values in CommandLine or as SPRING_APPLICATION_JSON Environment variable
        /// </summary>
        /// <param name="builder"><see cref="WebApplicationBuilder"/></param>
        public static WebApplicationBuilder AddSpringBootConfiguration(this WebApplicationBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Configuration.AddSpringBootEnv();
            builder.Configuration.AddSpringBootCmd(builder.Configuration);
            return builder;
        }
#endif
    }
}
