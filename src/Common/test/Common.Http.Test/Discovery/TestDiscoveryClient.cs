// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Discovery;

namespace Steeltoe.Common.Http.Test.Discovery;

internal sealed class TestDiscoveryClient : IDiscoveryClient
{
    private readonly IServiceInstance _instance;

    public string Description => throw new NotImplementedException();

    public TestDiscoveryClient(IServiceInstance instance = null)
    {
        _instance = instance;
    }

    public Task<IList<string>> GetServiceIdsAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IList<IServiceInstance>> GetInstancesAsync(string serviceId, CancellationToken cancellationToken)
    {
        IList<IServiceInstance> instances = new List<IServiceInstance>();

        if (_instance != null)
        {
            instances.Add(_instance);
        }

        return Task.FromResult(instances);
    }

    public IServiceInstance GetLocalServiceInstance()
    {
        throw new NotImplementedException();
    }

    public Task ShutdownAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
