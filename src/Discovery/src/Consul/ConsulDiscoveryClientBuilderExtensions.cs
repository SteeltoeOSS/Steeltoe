// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Client;
using Steeltoe.Discovery.Consul.Discovery;

namespace Steeltoe.Discovery.Consul;

public static class ConsulDiscoveryClientBuilderExtensions
{
    /// <summary>
    /// Configures <see cref="ConsulDiscoveryClient" /> as the <see cref="IDiscoveryClient" /> of choice.
    /// </summary>
    /// <param name="clientBuilder">
    /// <see cref="DiscoveryClientBuilder" />.
    /// </param>
    public static DiscoveryClientBuilder UseConsul(this DiscoveryClientBuilder clientBuilder)
    {
        clientBuilder.Extensions.Add(new ConsulDiscoveryClientExtension());
        return clientBuilder;
    }
}
