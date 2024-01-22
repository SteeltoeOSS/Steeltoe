// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Steeltoe.Common;
using Steeltoe.Common.Discovery;

namespace Steeltoe.Discovery.Client.SimpleClients;

public static class ConfigurationDiscoveryClientBuilderExtensions
{
    /// <summary>
    /// Configures <see cref="ConfigurationDiscoveryClient" /> as the <see cref="IDiscoveryClient" /> of choice. Reads service instances from app
    /// configuration, instead of a hosted service registry.
    /// </summary>
    /// <param name="clientBuilder">
    /// The builder to register configuration-based discovery on.
    /// </param>
    /// <remarks>
    /// Build your list of service instances under the configuration prefix discovery:services.
    /// <example>
    /// Example configuration in appsettings.json:
    /// <![CDATA[
    /// {
    ///   "discovery": {
    ///     "services": [
    ///       {
    ///         "serviceId": "CartService",
    ///         "host": "knownhost1",
    ///         "port": 443,
    ///         "isSecure": true
    ///       }, {
    ///         "serviceId": "CartService",
    ///         "host": "knownhost2",
    ///         "port": 443,
    ///         "isSecure": true
    ///       },
    ///     ]
    ///   }
    /// }
    /// ]]>
    /// </example>
    /// </remarks>
    public static DiscoveryClientBuilder UseConfiguration(this DiscoveryClientBuilder clientBuilder)
    {
        ArgumentGuard.NotNull(clientBuilder);

        clientBuilder.Extensions.Add(new ConfigurationDiscoveryClientExtension());
        return clientBuilder;
    }
}
