// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Steeltoe.Connectors.CloudFoundry;
using Steeltoe.Discovery.Client.SimpleClients;

namespace Steeltoe.Discovery.Client;

public static class DiscoveryWebHostBuilderExtensions
{
    /// <summary>
    /// Adds service discovery to your application. This method can be used in place of configuration via your Startup class.
    /// <para />
    /// Uses reflection to find discovery client packages. If no package is found, a <see cref="NoOpDiscoveryClient" /> will be configured.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    /// <remarks>
    /// Also configures named HttpClients "DiscoveryRandom" and "DiscoveryRoundRobin" for automatic injection.
    /// </remarks>
    /// <exception cref="AmbiguousMatchException">
    /// Thrown if multiple IDiscoveryClient implementations are configured.
    /// </exception>
    /// <exception cref="ConnectorException">
    /// Thrown if no service info with expected name or type are found or when multiple service infos are found and a single was expected.
    /// </exception>
    public static IWebHostBuilder AddDiscoveryClient(this IWebHostBuilder hostBuilder)
    {
        return hostBuilder.ConfigureServices((context, services) => services.AddDiscoveryClient(context.Configuration));
    }

    /// <summary>
    /// Adds service discovery to your application. This method can be used in place of configuration via your Startup class.
    /// <para />
    /// If <paramref name="optionsAction" /> is not provided, a <see cref="NoOpDiscoveryClient" /> will be configured.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    /// <param name="optionsAction">
    /// Select the discovery client implementation.
    /// </param>
    /// <remarks>
    /// Also configures named HttpClients "DiscoveryRandom" and "DiscoveryRoundRobin" for automatic injection.
    /// </remarks>
    /// <exception cref="AmbiguousMatchException">
    /// Thrown if multiple IDiscoveryClient implementations are configured.
    /// </exception>
    /// <exception cref="ConnectorException">
    /// Thrown if no service info with expected name or type are found or when multiple service infos are found and a single was expected.
    /// </exception>
    public static IWebHostBuilder AddServiceDiscovery(this IWebHostBuilder hostBuilder, Action<DiscoveryClientBuilder> optionsAction)
    {
        return hostBuilder.ConfigureServices((context, services) => services.AddServiceDiscovery(context.Configuration, optionsAction));
    }
}
