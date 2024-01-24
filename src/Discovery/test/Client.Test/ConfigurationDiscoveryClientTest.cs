// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Discovery;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Client.SimpleClients;
using Xunit;

namespace Steeltoe.Discovery.Client.Test;

public sealed class ConfigurationDiscoveryClientTest
{
    [Fact]
    public async Task Returns_ConfiguredServices()
    {
        var options = new ConfigurationDiscoveryOptions
        {
            Services =
            {
                new ConfigurationServiceInstance
                {
                    ServiceId = "fruitService",
                    Host = "fruitball",
                    Port = 443,
                    IsSecure = true
                },
                new ConfigurationServiceInstance
                {
                    ServiceId = "fruitService",
                    Host = "fruitballer",
                    Port = 8081
                },
                new ConfigurationServiceInstance
                {
                    ServiceId = "fruitService",
                    Host = "fruitballerz",
                    Port = 8082
                },
                new ConfigurationServiceInstance
                {
                    ServiceId = "vegetableService",
                    Host = "vegemite",
                    Port = 443,
                    IsSecure = true
                },
                new ConfigurationServiceInstance
                {
                    ServiceId = "vegetableService",
                    Host = "carrot",
                    Port = 8081
                },
                new ConfigurationServiceInstance
                {
                    ServiceId = "vegetableService",
                    Host = "beet",
                    Port = 8082
                }
            }
        };

        var optionsMonitor = new TestOptionsMonitor<ConfigurationDiscoveryOptions>(options);
        var client = new ConfigurationDiscoveryClient(optionsMonitor);

        IList<IServiceInstance> fruitInstances = await client.GetInstancesAsync("fruitService", CancellationToken.None);
        Assert.Equal(3, fruitInstances.Count);

        IList<IServiceInstance> vegetableInstances = await client.GetInstancesAsync("vegetableService", CancellationToken.None);
        Assert.Equal(3, vegetableInstances.Count);

        IList<string> servicesIds = await client.GetServiceIdsAsync(CancellationToken.None);
        Assert.Equal(2, servicesIds.Count);
    }

    [Fact]
    public async Task ReceivesUpdatesTo_ConfiguredServices()
    {
        var options = new ConfigurationDiscoveryOptions
        {
            Services =
            {
                new ConfigurationServiceInstance
                {
                    ServiceId = "fruitService",
                    Host = "fruitball",
                    Port = 443,
                    IsSecure = true
                }
            }
        };

        var optionsMonitor = new TestOptionsMonitor<ConfigurationDiscoveryOptions>(options);
        var client = new ConfigurationDiscoveryClient(optionsMonitor);

        IList<IServiceInstance> fruitInstances = await client.GetInstancesAsync("fruitService", CancellationToken.None);

        Assert.Single(fruitInstances);
        Assert.Equal("fruitball", fruitInstances[0].Host);

        options.Services[0].Host = "updatedValue";

        Assert.Equal("updatedValue", fruitInstances[0].Host);
    }
}
