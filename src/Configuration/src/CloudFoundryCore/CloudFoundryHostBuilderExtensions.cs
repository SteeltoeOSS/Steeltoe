// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;

namespace Steeltoe.Extensions.Configuration.CloudFoundry;

public static class CloudFoundryHostBuilderExtensions
{
    /// <summary>
    /// Add Cloud Foundry Configuration Provider
    /// </summary>
    /// <param name="hostBuilder">Your HostBuilder</param>
    [Obsolete("This method has been removed, please use AddCloudFoundryConfiguration instead", true)]
    public static IWebHostBuilder AddCloudFoundry(this IWebHostBuilder hostBuilder) =>
        hostBuilder.AddCloudFoundryConfiguration();

    /// <summary>
    /// Add Cloud Foundry Configuration Provider
    /// </summary>
    /// <param name="hostBuilder">Your WebHostBuilder</param>
    [Obsolete("This method has been removed, please use AddCloudFoundryConfiguration instead", true)]
    public static IHostBuilder AddCloudFoundry(this IHostBuilder hostBuilder) =>
        hostBuilder.AddCloudFoundryConfiguration();

    /// <summary>
    /// Add Cloud Foundry Configuration Provider
    /// </summary>
    /// <param name="hostBuilder">Your HostBuilder</param>
    public static IWebHostBuilder AddCloudFoundryConfiguration(this IWebHostBuilder hostBuilder) =>
        hostBuilder
            .ConfigureAppConfiguration((context, config) => { config.AddCloudFoundry(); })
            .ConfigureServices((context, serviceCollection) => serviceCollection.RegisterCloudFoundryApplicationInstanceInfo());

    /// <summary>
    /// Add Cloud Foundry Configuration Provider
    /// </summary>
    /// <param name="hostBuilder">Your WebHostBuilder</param>
    public static IHostBuilder AddCloudFoundryConfiguration(this IHostBuilder hostBuilder) =>
        hostBuilder
            .ConfigureAppConfiguration((context, config) => { config.AddCloudFoundry(); })
            .ConfigureServices((context, serviceCollection) => serviceCollection.RegisterCloudFoundryApplicationInstanceInfo());

#if NET6_0_OR_GREATER
        /// <summary>
        /// Add Cloud Foundry Configuration Provider
        /// </summary>
        /// <param name="applicationBuilder">Your <see cref="WebApplicationBuilder"/></param>
        public static WebApplicationBuilder AddCloudFoundryConfiguration(this WebApplicationBuilder applicationBuilder)
        {
            applicationBuilder.Configuration.AddCloudFoundry();
            applicationBuilder.Services.RegisterCloudFoundryApplicationInstanceInfo();
            return applicationBuilder;
        }
#endif
}