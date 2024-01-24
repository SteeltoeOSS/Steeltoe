// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Hosting;
using Steeltoe.Common;
using Steeltoe.Common.Discovery;

namespace Steeltoe.Discovery.Client;

internal sealed class DiscoveryClientService : IHostedService
{
    private readonly IDiscoveryClient _client;

    public DiscoveryClientService(IDiscoveryClient client)
    {
        ArgumentGuard.NotNull(client);

        _client = client;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return _client.ShutdownAsync(cancellationToken);
    }
}
