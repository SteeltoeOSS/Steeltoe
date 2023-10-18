// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Discovery;
using Steeltoe.Discovery;

namespace Steeltoe.Configuration.ConfigServer.Test;

internal sealed class TestDiscoveryClient : IDiscoveryClient
{
    internal bool HasShutdown { get; private set; }

    public string Description => throw new NotImplementedException();

    public Task<IList<string>> GetServicesAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IList<IServiceInstance>> GetInstancesAsync(string serviceId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IServiceInstance> GetLocalServiceInstanceAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task ShutdownAsync(CancellationToken cancellationToken)
    {
        HasShutdown = true;
        return Task.CompletedTask;
    }
}
