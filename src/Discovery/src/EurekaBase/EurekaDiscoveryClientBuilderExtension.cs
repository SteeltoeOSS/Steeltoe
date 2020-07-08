// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Client;

namespace Steeltoe.Discovery.Eureka
{
    public static class EurekaDiscoveryClientBuilderExtension
    {
        /// <summary>
        /// Configures <see cref="EurekaDiscoveryClient"/> as the <see cref="IDiscoveryClient"/> of choice
        /// </summary>
        /// <param name="clientBuilder"><see cref="DiscoveryClientBuilder"/></param>
        /// <param name="serviceInfoName">Optionally specify the name of a specific Eureka service binding to look for</param>
        public static DiscoveryClientBuilder UseEureka(this DiscoveryClientBuilder clientBuilder, string serviceInfoName = null)
        {
            clientBuilder.Extensions.Add(new EurekaDiscoveryClientExtension(serviceInfoName));
            return clientBuilder;
        }
    }
}
