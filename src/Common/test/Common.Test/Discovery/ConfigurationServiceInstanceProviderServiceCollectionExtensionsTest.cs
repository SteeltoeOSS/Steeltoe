// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Utils.IO;
using Xunit;

namespace Steeltoe.Common.Discovery.Test;

public class ConfigurationServiceInstanceProviderServiceCollectionExtensionsTest
{
    [Fact]
    public void AddConfigurationDiscoveryClient_AddsClientWithOptions()
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
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // by getting the provider, we're confirming that the options are also available in the container
        var serviceInstanceProvider = serviceProvider.GetRequiredService(typeof(IServiceInstanceProvider)) as IServiceInstanceProvider;

        Assert.NotNull(serviceInstanceProvider);
        Assert.IsType<ConfigurationServiceInstanceProvider>(serviceInstanceProvider);
        Assert.Equal(2, serviceInstanceProvider.Services.Count);
        Assert.Equal(2, serviceInstanceProvider.GetInstances("fruitService").Count);
        Assert.Equal(2, serviceInstanceProvider.GetInstances("vegetableService").Count);
    }
}
