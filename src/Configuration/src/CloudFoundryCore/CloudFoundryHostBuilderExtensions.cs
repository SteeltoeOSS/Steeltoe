// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Steeltoe.Extensions.Configuration.CloudFoundry
{
    public static class CloudFoundryHostBuilderExtensions
    {
        /// <summary>
        /// Add Cloud Foundry Configuration Provider
        /// </summary>
        /// <param name="hostBuilder">Your HostBuilder</param>
        public static IWebHostBuilder AddCloudFoundry(this IWebHostBuilder hostBuilder)
        {
            hostBuilder
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddCloudFoundry();
                })
                .ConfigureServices(serviceCollection => serviceCollection.RegisterCloudFoundryApplicationInstanceInfo());

            return hostBuilder;
        }

        /// <summary>
        /// Add Cloud Foundry Configuration Provider
        /// </summary>
        /// <param name="hostBuilder">Your WebHostBuilder</param>
        public static IHostBuilder AddCloudFoundry(this IHostBuilder hostBuilder)
        {
            hostBuilder
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddCloudFoundry();
                })
                .ConfigureServices((context, serviceCollection) => serviceCollection.RegisterCloudFoundryApplicationInstanceInfo());

            return hostBuilder;
        }
    }
}
