// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if NET6_0_OR_GREATER
using Microsoft.AspNetCore.Builder;
using Steeltoe.Connector;
using Steeltoe.Discovery.Client.SimpleClients;
using System;
using System.Reflection;

namespace Steeltoe.Discovery.Client;

public static class DiscoveryWebApplicationBuilderExtensions
{
    /// <summary>
    /// Adds service discovery to your application. This method can be used in place of configuration via your Startup class.<para />
    /// Uses reflection to find discovery client packages. If no package is found, a <see cref="NoOpDiscoveryClient"/> will be configured.
    /// </summary>
    /// <param name="hostBuilder">Your HostBuilder.</param>
    /// <remarks>Also configures named HttpClients "DiscoveryRandom" and "DiscoveryRoundRobin" for automatic injection.</remarks>
    /// <exception cref="AmbiguousMatchException">Thrown if multiple IDiscoveryClient implementations are configured.</exception>
    /// <exception cref="ConnectorException">Thrown if no service info with expected name or type are found or when multiple service infos are found and a single was expected.</exception>
    public static WebApplicationBuilder AddDiscoveryClient(this WebApplicationBuilder hostBuilder)
    {
        hostBuilder.Services.AddDiscoveryClient(hostBuilder.Configuration);
        return hostBuilder;
    }

    /// <summary>
    /// Adds service discovery to your application. This method can be used in place of configuration via your Startup class.<para />
    /// If <paramref name="optionsAction"/> is not provided, a <see cref="NoOpDiscoveryClient"/> will be configured.
    /// </summary>
    /// <param name="hostBuilder">Your HostBuilder.</param>
    /// <param name="optionsAction">Select the discovery client implementation.</param>
    /// <remarks>Also configures named HttpClients "DiscoveryRandom" and "DiscoveryRoundRobin" for automatic injection.</remarks>
    /// <exception cref="AmbiguousMatchException">Thrown if multiple IDiscoveryClient implementations are configured.</exception>
    /// <exception cref="ConnectorException">Thrown if no service info with expected name or type are found or when multiple service infos are found and a single was expected.</exception>
    public static WebApplicationBuilder AddServiceDiscovery(this WebApplicationBuilder hostBuilder, Action<DiscoveryClientBuilder> optionsAction)
    {
        hostBuilder.Services.AddServiceDiscovery(optionsAction);
        return hostBuilder;
    }
}
#endif
