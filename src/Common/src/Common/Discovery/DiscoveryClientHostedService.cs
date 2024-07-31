// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Hosting;

namespace Steeltoe.Common.Discovery;

/// <summary>
/// Calls <see cref="IDiscoveryClient.ShutdownAsync" /> when the app is being stopped.
/// </summary>
internal sealed class DiscoveryClientHostedService : IHostedService
{
    private readonly ICollection<IDiscoveryClient> _discoveryClients;

    public DiscoveryClientHostedService(IEnumerable<IDiscoveryClient> discoveryClients)
    {
        ArgumentGuard.NotNull(discoveryClients);

        _discoveryClients = discoveryClients.ToArray();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (IDiscoveryClient discoveryClient in _discoveryClients)
        {
            await discoveryClient.ShutdownAsync(cancellationToken);
        }
    }
}
