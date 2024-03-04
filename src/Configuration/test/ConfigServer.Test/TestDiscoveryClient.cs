// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Discovery;
using Steeltoe.Discovery;

namespace Steeltoe.Configuration.ConfigServer.Test;

internal sealed class TestDiscoveryClient : IDiscoveryClient
{
    internal bool HasShutdown { get; private set; }

    public string Description => string.Empty;

    public Task<IList<string>> GetServicesAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IList<string>>(Array.Empty<string>());
    }

    public Task<IList<IServiceInstance>> GetInstancesAsync(string serviceId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IList<IServiceInstance>>(Array.Empty<IServiceInstance>());
    }

    public Task<IServiceInstance> GetLocalServiceInstanceAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IServiceInstance>(new JsonSerializableServiceInstance());
    }

    public Task ShutdownAsync(CancellationToken cancellationToken)
    {
        HasShutdown = true;
        return Task.CompletedTask;
    }
}
