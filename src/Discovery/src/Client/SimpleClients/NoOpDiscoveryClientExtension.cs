// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common;
using Steeltoe.Common.Discovery;
using Steeltoe.Connectors.Services;

namespace Steeltoe.Discovery.Client.SimpleClients;

internal sealed class NoOpDiscoveryClientExtension : IDiscoveryClientExtension
{
    /// <inheritdoc />
    public bool IsConfigured(IConfiguration configuration, IServiceInfo? serviceInfo)
    {
        return false;
    }

    /// <inheritdoc />
    public void ApplyServices(IServiceCollection services)
    {
        ArgumentGuard.NotNull(services);

        services.AddSingleton<IDiscoveryClient, NoOpDiscoveryClient>();
    }
}
