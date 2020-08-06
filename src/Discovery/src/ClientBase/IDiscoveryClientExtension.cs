// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Connector.Services;

namespace Steeltoe.Discovery.Client
{
    public interface IDiscoveryClientExtension
    {
        /// <summary>
        /// Check if this client has been configured
        /// </summary>
        /// <param name="configuration">Application Configuration to search</param>
        /// <param name="serviceInfo">Service binding credentials</param>
        /// <returns>Value indicating presence of expected configuration keys</returns>
        bool IsConfigured(IConfiguration configuration, IServiceInfo serviceInfo = null);

        /// <summary>
        /// Implementations of this method will add services required by the <see cref="IDiscoveryClient"/> to the service collection for activation later
        /// </summary>
        /// <param name="services"><see cref="IServiceCollection"/> to configure</param>
        void ApplyServices(IServiceCollection services);
    }
}