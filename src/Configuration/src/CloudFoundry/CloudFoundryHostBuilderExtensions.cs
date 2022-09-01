// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common;

namespace Steeltoe.Extensions.Configuration.CloudFoundry;

public static class CloudFoundryHostBuilderExtensions
{
    /// <summary>
    /// Adds the Cloud Foundry configuration provider.
    /// </summary>
    /// <param name="hostBuilder">
    /// The host builder.
    /// </param>
    public static IWebHostBuilder AddCloudFoundryConfiguration(this IWebHostBuilder hostBuilder)
    {
        ArgumentGuard.NotNull(hostBuilder);

        hostBuilder.ConfigureAppConfiguration((_, builder) => builder.AddCloudFoundry());
        hostBuilder.ConfigureServices((_, serviceCollection) => serviceCollection.RegisterCloudFoundryApplicationInstanceInfo());

        return hostBuilder;
    }

    /// <summary>
    /// Adds the Cloud Foundry configuration provider.
    /// </summary>
    /// <param name="hostBuilder">
    /// The host builder.
    /// </param>
    public static IHostBuilder AddCloudFoundryConfiguration(this IHostBuilder hostBuilder)
    {
        ArgumentGuard.NotNull(hostBuilder);

        hostBuilder.ConfigureAppConfiguration((_, builder) => builder.AddCloudFoundry());
        hostBuilder.ConfigureServices((_, serviceCollection) => serviceCollection.RegisterCloudFoundryApplicationInstanceInfo());

        return hostBuilder;
    }

    /// <summary>
    /// Adds the Cloud Foundry configuration provider.
    /// </summary>
    /// <param name="applicationBuilder">
    /// The application builder.
    /// </param>
    public static WebApplicationBuilder AddCloudFoundryConfiguration(this WebApplicationBuilder applicationBuilder)
    {
        ArgumentGuard.NotNull(applicationBuilder);

        applicationBuilder.Configuration.AddCloudFoundry();
        applicationBuilder.Services.RegisterCloudFoundryApplicationInstanceInfo();

        return applicationBuilder;
    }
}
