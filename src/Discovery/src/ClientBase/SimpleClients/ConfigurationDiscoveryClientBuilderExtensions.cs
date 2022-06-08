// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Discovery.Client.SimpleClients;

public static class ConfigurationDiscoveryClientBuilderExtensions
{
    /// <summary>
    /// Allows the use of IDiscoveryClient model built from IConfiguration instead of a hosted service registry
    /// </summary>
    /// <param name="clientBuilder">this</param>
    /// <remarks>
    ///     Build your list of service instances under the configuration prefix discovery:services<para></para>
    ///     For example:
    ///       "discovery": {
    ///         "services": [
    ///           { "serviceId": "CartService", "host": "knownhost1", "port": 443, "isSecure": true },
    ///           { "serviceId": "CartService", "host": "knownhost2", "port": 443, "isSecure": true },
    ///         ]
    ///       }
    /// </remarks>
    public static DiscoveryClientBuilder UseConfiguredInstances(this DiscoveryClientBuilder clientBuilder)
    {
        clientBuilder.Extensions.Add(new ConfigurationDiscoveryClientExtension());
        return clientBuilder;
    }
}
