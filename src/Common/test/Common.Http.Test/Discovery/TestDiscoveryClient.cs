// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Discovery;
using Steeltoe.Discovery;

namespace Steeltoe.Common.Http.Test.Discovery;

internal sealed class TestDiscoveryClient : IDiscoveryClient
{
    private readonly IServiceInstance _instance;

    public string Description => throw new NotImplementedException();

    public IList<string> Services => throw new NotImplementedException();

    public TestDiscoveryClient(IServiceInstance instance = null)
    {
        _instance = instance;
    }

    public IList<IServiceInstance> GetInstances(string serviceId)
    {
        if (_instance != null)
        {
            return new List<IServiceInstance>
            {
                _instance
            };
        }

        return new List<IServiceInstance>();
    }

    public IServiceInstance GetLocalServiceInstance()
    {
        throw new NotImplementedException();
    }

    public Task ShutdownAsync()
    {
        throw new NotImplementedException();
    }
}
