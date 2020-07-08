// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Client;

namespace Steeltoe.Discovery.Eureka
{
    public static class EurekaDiscoveryClientBuilderExtension
    {
        public static DiscoveryClientBuilder UseEureka(this DiscoveryClientBuilder clientBuilder, string serviceInfoName = null)
        {
            clientBuilder.Extensions.Add(new EurekaDiscoveryClientExtension(serviceInfoName));
            return clientBuilder;
        }
    }
}
