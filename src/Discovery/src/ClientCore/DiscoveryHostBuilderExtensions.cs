// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Steeltoe.Discovery.Client
{
    public static class DiscoveryHostBuilderExtensions
    {
        /// <summary>
        /// Adds service discovery to your application based on app configuration. This method can be used in place of configuration via your Startup class.
        /// </summary>
        /// <param name="hostBuilder">Your HostBuilder</param>
        public static IWebHostBuilder AddServiceDiscovery(this IWebHostBuilder hostBuilder)
        {
            return hostBuilder.ConfigureServices((context, collection) => AddServices(collection, context.Configuration));
        }

        /// <summary>
        /// Adds service discovery to your application based on app configuration. This method can be used in place of configuration via your Startup class.
        /// </summary>
        /// <param name="hostBuilder">Your HostBuilder</param>
        public static IHostBuilder AddServiceDiscovery(this IHostBuilder hostBuilder)
        {
            return hostBuilder.ConfigureServices((context, collection) => AddServices(collection, context.Configuration));
        }

        private static void AddServices(IServiceCollection collection, IConfiguration config)
        {
            collection.AddDiscoveryClient(config);
            collection.AddTransient<IStartupFilter, DiscoveryClientStartupFilter>();
            collection.AddHttpClient("DiscoveryRandom").AddRandomLoadBalancer();
            collection.AddHttpClient("DiscoveryRoundRobin").AddRoundRobinLoadBalancer();
        }
    }
}
