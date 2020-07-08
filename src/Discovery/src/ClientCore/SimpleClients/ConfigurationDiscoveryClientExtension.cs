// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Discovery;
using System.Collections.Generic;

namespace Steeltoe.Discovery.Client.SimpleClients
{
    public class ConfigurationDiscoveryClientExtension : IDiscoveryClientExtension
    {
        /// <inheritdoc/>
        public void ApplyServices(IServiceCollection services)
        {
            var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
            services.Configure<List<ConfigurationServiceInstance>>(configuration.GetSection("discovery:services"));
            services.AddSingleton<IDiscoveryClient>((serviceProvider) => new ConfigurationDiscoveryClient(serviceProvider.GetRequiredService<IOptionsMonitor<List<ConfigurationServiceInstance>>>()));
        }
    }
}
