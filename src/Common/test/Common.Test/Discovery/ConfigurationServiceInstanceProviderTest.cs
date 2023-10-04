// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Discovery;
using Steeltoe.Common.TestResources;
using Xunit;

namespace Steeltoe.Common.Test.Discovery;

public sealed class ConfigurationServiceInstanceProviderTest
{
    [Fact]
    public async Task Returns_ConfiguredServices()
    {
        var services = new List<ConfigurationServiceInstance>
        {
            new()
            {
                ServiceId = "fruitService",
                Host = "fruitball",
                Port = 443,
                IsSecure = true
            },
            new()
            {
                ServiceId = "fruitService",
                Host = "fruitballer",
                Port = 8081
            },
            new()
            {
                ServiceId = "fruitService",
                Host = "fruitballerz",
                Port = 8082
            },
            new()
            {
                ServiceId = "vegetableService",
                Host = "vegemite",
                Port = 443,
                IsSecure = true
            },
            new()
            {
                ServiceId = "vegetableService",
                Host = "carrot",
                Port = 8081
            },
            new()
            {
                ServiceId = "vegetableService",
                Host = "beet",
                Port = 8082
            }
        };

        var serviceOptions = new TestOptionsMonitor<List<ConfigurationServiceInstance>>(services);

        var provider = new ConfigurationServiceInstanceProvider(serviceOptions);

        IList<IServiceInstance> fruitInstances = await provider.GetInstancesAsync("fruitService", CancellationToken.None);
        Assert.Equal(3, fruitInstances.Count);

        IList<IServiceInstance> vegetableInstances = await provider.GetInstancesAsync("vegetableService", CancellationToken.None);
        Assert.Equal(3, vegetableInstances.Count);

        IList<string> servicesIds = await provider.GetServicesAsync(CancellationToken.None);
        Assert.Equal(2, servicesIds.Count);
    }

    [Fact]
    public async Task ReceivesUpdatesTo_ConfiguredServices()
    {
        var services = new List<ConfigurationServiceInstance>
        {
            new()
            {
                ServiceId = "fruitService",
                Host = "fruitball",
                Port = 443,
                IsSecure = true
            }
        };

        var serviceOptions = new TestOptionsMonitor<List<ConfigurationServiceInstance>>(services);
        var provider = new ConfigurationServiceInstanceProvider(serviceOptions);

        IList<IServiceInstance> fruitInstances = await provider.GetInstancesAsync("fruitService", CancellationToken.None);

        Assert.Single(fruitInstances);
        Assert.Equal("fruitball", fruitInstances[0].Host);

        services[0].Host = "updatedValue";

        Assert.Equal("updatedValue", fruitInstances[0].Host);
    }
}
