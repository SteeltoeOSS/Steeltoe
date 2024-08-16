// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;

namespace Steeltoe.Discovery.Eureka;

/// <summary>
/// Workaround for the circular dependency between <see cref="EurekaHealthCheckHandler" /> and <see cref="EurekaDiscoveryClient" />.
/// </summary>
public sealed class HealthCheckHandlerProvider
{
    private readonly IServiceProvider _serviceProvider;

    public HealthCheckHandlerProvider(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        _serviceProvider = serviceProvider;
    }

    internal IHealthCheckHandler GetHandler()
    {
        return _serviceProvider.GetRequiredService<IHealthCheckHandler>();
    }
}
