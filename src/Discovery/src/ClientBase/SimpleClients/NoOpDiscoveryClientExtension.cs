// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Connector.Services;

namespace Steeltoe.Discovery.Client.SimpleClients;

internal sealed class NoOpDiscoveryClientExtension : IDiscoveryClientExtension
{
    /// <inheritdoc/>
    public void ApplyServices(IServiceCollection services)
    {
        services.AddSingleton<IDiscoveryClient>(services => new NoOpDiscoveryClient(services.GetRequiredService<IConfiguration>(), services.GetService<ILogger<NoOpDiscoveryClient>>()));
    }

    public bool IsConfigured(IConfiguration configuration, IServiceInfo serviceInfo = null)
    {
        return false;
    }
}
