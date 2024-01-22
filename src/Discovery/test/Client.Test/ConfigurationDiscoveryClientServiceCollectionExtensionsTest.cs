// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.Utils.IO;
using Steeltoe.Discovery.Client.SimpleClients;
using Xunit;

namespace Steeltoe.Discovery.Client.Test;

public sealed class ConfigurationServiceInstanceProviderServiceCollectionExtensionsTest
{
    [Fact]
    public async Task AddConfigurationDiscoveryClient_AddsClientWithOptions()
    {
        const string appsettings = @"
{
    ""discovery"": {
        ""services"": [
            { ""serviceId"": ""fruitService"", ""host"": ""fruitball"", ""port"": 443, ""isSecure"": true },
            { ""serviceId"": ""fruitService"", ""host"": ""fruitballer"", ""port"": 8081 },
            { ""serviceId"": ""vegetableService"", ""host"": ""vegemite"", ""port"": 443, ""isSecure"": true },
            { ""serviceId"": ""vegetableService"", ""host"": ""carrot"", ""port"": 8081 },
        ]
    }
}";

        using var sandbox = new Sandbox();
        string path = sandbox.CreateFile("appsettings.json", appsettings);
        string directory = Path.GetDirectoryName(path);
        string fileName = Path.GetFileName(path);
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(directory);
        configurationBuilder.AddJsonFile(fileName);
        IConfigurationRoot configuration = configurationBuilder.Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddServiceDiscovery(configuration, builder => builder.UseConfiguration());

        ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        // by getting the client, we're confirming that the options are also available in the container
        var client = serviceProvider.GetRequiredService<IDiscoveryClient>();

        Assert.IsType<ConfigurationDiscoveryClient>(client);

        IList<string> servicesIds = await client.GetServiceIdsAsync(CancellationToken.None);
        Assert.Equal(2, servicesIds.Count);

        IList<IServiceInstance> fruitInstances = await client.GetInstancesAsync("fruitService", CancellationToken.None);
        Assert.Equal(2, fruitInstances.Count);

        IList<IServiceInstance> vegetableInstances = await client.GetInstancesAsync("vegetableService", CancellationToken.None);
        Assert.Equal(2, vegetableInstances.Count);
    }
}
