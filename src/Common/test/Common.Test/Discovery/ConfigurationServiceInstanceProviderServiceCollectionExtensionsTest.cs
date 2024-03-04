// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.Utils.IO;
using Xunit;

namespace Steeltoe.Common.Test.Discovery;

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
        var builder = new ConfigurationBuilder();
        builder.SetBasePath(directory);
        builder.AddJsonFile(fileName);
        var services = new ServiceCollection();

        services.AddConfigurationDiscoveryClient(builder.Build());
        ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        // by getting the provider, we're confirming that the options are also available in the container
        var serviceInstanceProvider = serviceProvider.GetRequiredService<IServiceInstanceProvider>();

        Assert.NotNull(serviceInstanceProvider);
        Assert.IsType<ConfigurationServiceInstanceProvider>(serviceInstanceProvider);

        IList<string> servicesIds = await serviceInstanceProvider.GetServicesAsync(CancellationToken.None);
        Assert.Equal(2, servicesIds.Count);

        IList<IServiceInstance> fruitInstances = await serviceInstanceProvider.GetInstancesAsync("fruitService", CancellationToken.None);
        Assert.Equal(2, fruitInstances.Count);

        IList<IServiceInstance> vegetableInstances = await serviceInstanceProvider.GetInstancesAsync("vegetableService", CancellationToken.None);
        Assert.Equal(2, vegetableInstances.Count);
    }
}
