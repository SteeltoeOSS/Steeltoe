// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace Steeltoe.Discovery.Client
{
    public static class DiscoveryHostBuilderExtensions
    {
        /// <summary>
        /// Adds service discovery to your application based on app configuration. This method can be used in place of configuration via your Startup class.
        /// </summary>
        /// <param name="hostBuilder">Your HostBuilder</param>
        /// <param name="optionsAction">Select the discovery client implementation</param>
        public static IWebHostBuilder AddServiceDiscovery(this IWebHostBuilder hostBuilder, Action<DiscoveryClientBuilder> optionsAction)
        {
            return hostBuilder.ConfigureServices((context, collection) => AddServices(collection, optionsAction));
        }

        /// <summary>
        /// Adds service discovery to your application based on app configuration. This method can be used in place of configuration via your Startup class.
        /// </summary>
        /// <param name="hostBuilder">Your HostBuilder</param>
        /// <param name="optionsAction">Select the discovery client implementation</param>
        /// <remarks>Also configures named HttpClients "DiscoveryRandom" and "DiscoveryRoundRobin" for automatic injection</remarks>
        public static IHostBuilder AddServiceDiscovery(this IHostBuilder hostBuilder, Action<DiscoveryClientBuilder> optionsAction)
        {
            return hostBuilder.ConfigureServices((context, collection) => AddServices(collection, optionsAction));
        }

        private static void AddServices(IServiceCollection collection, Action<DiscoveryClientBuilder> optionsAction)
        {
            collection.AddServiceDiscovery(optionsAction);
            collection.AddTransient<IStartupFilter, DiscoveryClientStartupFilter>();
            collection.AddHttpClient("DiscoveryRandom").AddRandomLoadBalancer();
            collection.AddHttpClient("DiscoveryRoundRobin").AddRoundRobinLoadBalancer();
        }
    }
}