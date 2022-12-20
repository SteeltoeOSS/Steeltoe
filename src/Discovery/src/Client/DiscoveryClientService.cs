// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Hosting;

namespace Steeltoe.Discovery.Client;

internal sealed class DiscoveryClientService : IHostedService
{
    private readonly IDiscoveryClient _discoveryClient;

    public DiscoveryClientService(IDiscoveryClient client, IDiscoveryLifecycle applicationLifetime = null)
    {
        _discoveryClient = client;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return _discoveryClient.ShutdownAsync();
    }
}
