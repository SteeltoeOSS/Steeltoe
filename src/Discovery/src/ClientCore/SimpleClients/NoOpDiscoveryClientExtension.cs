// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Discovery.Client.SimpleClients
{
    internal class NoOpDiscoveryClientExtension : IDiscoveryClientExtension
    {
        public void ApplyServices(IServiceCollection services)
        {
            services.AddSingleton<IDiscoveryClient>((services) => new NoOpDiscoveryClient(services.GetService<ILogger<NoOpDiscoveryClient>>()));
        }
    }
}
