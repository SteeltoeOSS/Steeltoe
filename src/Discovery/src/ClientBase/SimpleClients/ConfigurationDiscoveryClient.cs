// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using Steeltoe.Common.Discovery;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Steeltoe.Discovery.Client.SimpleClients;

public class ConfigurationDiscoveryClient : ConfigurationServiceInstanceProvider, IDiscoveryClient
{
    public ConfigurationDiscoveryClient(IOptionsMonitor<List<ConfigurationServiceInstance>> serviceInstances)
        : base(serviceInstances)
    {
    }

    public IServiceInstance GetLocalServiceInstance()
    {
        throw new NotImplementedException("No known use case for implementing this method");
    }

    public Task ShutdownAsync()
    {
        return Task.CompletedTask;
    }
}
