// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common;

namespace Steeltoe.Extensions.Configuration.SpringBoot;

public static class SpringBootHostBuilderExtensions
{
    /// <summary>
    /// Sets up the configuration provider in Spring Boot style '.' separated values from the command-line or the SPRING_APPLICATION_JSON environment
    /// variable.
    /// </summary>
    /// <param name="builder">
    /// The host builder.
    /// </param>
    /// <returns>
    /// <paramref name="builder" />.
    /// </returns>
    public static IHostBuilder AddSpringBootConfiguration(this IHostBuilder builder)
    {
        ArgumentGuard.NotNull(builder);

        return builder.ConfigureAppConfiguration((c, b) =>
        {
            b.AddSpringBootFromEnvironmentVariable();
            b.AddSpringBootFromCommandLine(c.Configuration);
        });
    }

    /// <summary>
    /// Sets up the configuration provider in Spring Boot style '.' separated values from the command-line or the SPRING_APPLICATION_JSON environment
    /// variable.
    /// </summary>
    /// <param name="builder">
    /// The host builder.
    /// </param>
    /// <returns>
    /// <paramref name="builder" />.
    /// </returns>
    public static IWebHostBuilder AddSpringBootConfiguration(this IWebHostBuilder builder)
    {
        ArgumentGuard.NotNull(builder);

        return builder.ConfigureAppConfiguration((context, configuration) =>
        {
            configuration.AddSpringBootFromEnvironmentVariable();
            configuration.AddSpringBootFromCommandLine(context.Configuration);
        });
    }

    /// <summary>
    /// Sets up the configuration provider in spring boot style '.' separated values from the command-line or the SPRING_APPLICATION_JSON environment
    /// variable.
    /// </summary>
    /// <param name="builder">
    /// The web application builder.
    /// </param>
    /// <returns>
    /// <paramref name="builder" />.
    /// </returns>
    public static WebApplicationBuilder AddSpringBootConfiguration(this WebApplicationBuilder builder)
    {
        ArgumentGuard.NotNull(builder);

        builder.Configuration.AddSpringBootFromEnvironmentVariable();
        builder.Configuration.AddSpringBootFromCommandLine(builder.Configuration);
        return builder;
    }
}
