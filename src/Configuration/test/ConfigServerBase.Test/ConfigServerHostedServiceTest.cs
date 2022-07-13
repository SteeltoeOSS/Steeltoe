// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Steeltoe.Extensions.Configuration.Placeholder;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Extensions.Configuration.ConfigServer.Test;

public class ConfigServerHostedServiceTest
{
    private static readonly MemoryConfigurationSource _fastTests = new () { InitialData = TestHelpers._fastTestsConfiguration };

    [Fact]
    public void Constructor_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => new ConfigServerHostedService(null, null));
    }

#if NET6_0_OR_GREATER
    [Fact]
    public async Task ServiceConstructsAndOperatesWithConfigurationManager()
    {
        var configurationManager = new ConfigurationManager();
        configurationManager.AddInMemoryCollection(TestHelpers._fastTestsConfiguration);
        configurationManager.AddConfigServer();
        var service = new ConfigServerHostedService(configurationManager, null);

        await service.StartAsync(default);
        await service.StopAsync(default);
        Assert.True(true, "Service constructed, started and stopped without exception");
    }
#endif

    [Fact]
    public async Task ServiceConstructsAndOperatesWithConfigurationRoot()
    {
        var configurationRoot = new ConfigurationRoot(
            new List<IConfigurationProvider>
            {
                new MemoryConfigurationProvider(_fastTests),
                new ConfigServerConfigurationProvider()
            });
        var service = new ConfigServerHostedService(configurationRoot, null);

        var startStopAction = async () =>
        {
            await service.StartAsync(default);
            await service.StopAsync(default);
        };

        await startStopAction.Should().NotThrowAsync("ConfigServerHostedService should start");
    }

    [Fact]
    public async Task FindsConfigServerProviderInPlaceholderProvider()
    {
        var placeholder = new PlaceholderResolverProvider(
            new List<IConfigurationProvider>
            {
                new MemoryConfigurationProvider(_fastTests),
                new ConfigServerConfigurationProvider()
            });
        var configRoot = new ConfigurationRoot(new List<IConfigurationProvider> { placeholder });
        var service = new ConfigServerHostedService(configRoot, null);

        var startStopAction = async () =>
        {
            await service.StartAsync(default);
            await service.StopAsync(default);
        };

        await startStopAction.Should().NotThrowAsync("ConfigServerHostedService should start");
    }
}
