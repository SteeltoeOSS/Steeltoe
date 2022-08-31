// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Steeltoe.Extensions.Configuration.CloudFoundry;

public static class CloudFoundryHostBuilderExtensions
{
    /// <summary>
    /// Add Cloud Foundry Configuration Provider.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    public static IWebHostBuilder AddCloudFoundryConfiguration(this IWebHostBuilder hostBuilder)
    {
        return hostBuilder.ConfigureAppConfiguration((_, builder) =>
        {
            builder.AddCloudFoundry();
        }).ConfigureServices((_, serviceCollection) => serviceCollection.RegisterCloudFoundryApplicationInstanceInfo());
    }

    /// <summary>
    /// Add Cloud Foundry Configuration Provider.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your WebHostBuilder.
    /// </param>
    public static IHostBuilder AddCloudFoundryConfiguration(this IHostBuilder hostBuilder)
    {
        return hostBuilder.ConfigureAppConfiguration((_, builder) =>
        {
            builder.AddCloudFoundry();
        }).ConfigureServices((_, serviceCollection) => serviceCollection.RegisterCloudFoundryApplicationInstanceInfo());
    }

    /// <summary>
    /// Add Cloud Foundry Configuration Provider.
    /// </summary>
    /// <param name="applicationBuilder">
    /// Your <see cref="WebApplicationBuilder" />.
    /// </param>
    public static WebApplicationBuilder AddCloudFoundryConfiguration(this WebApplicationBuilder applicationBuilder)
    {
        applicationBuilder.Configuration.AddCloudFoundry();
        applicationBuilder.Services.RegisterCloudFoundryApplicationInstanceInfo();
        return applicationBuilder;
    }
}
