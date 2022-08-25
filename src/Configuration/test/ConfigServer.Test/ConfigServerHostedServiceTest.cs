// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Steeltoe.Extensions.Configuration.Placeholder;
using Xunit;

namespace Steeltoe.Extensions.Configuration.ConfigServer.Test;

public sealed class ConfigServerHostedServiceTest
{
    [Fact]
    public void Constructor_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => new ConfigServerHostedService(null, null));
    }

    [Fact]
    public async Task ServiceConstructsAndOperatesWithConfigurationRoot()
    {
        var configurationRoot = new ConfigurationRoot(new List<IConfigurationProvider>
        {
            new ConfigServerConfigurationProvider(new ConfigServerClientSettings
            {
                Enabled = false
            })
        });

        var service = new ConfigServerHostedService(configurationRoot, null);

        Func<Task> startStopAction = async () =>
        {
            await service.StartAsync(default);
            await service.StopAsync(default);
        };

        await startStopAction.Should().NotThrowAsync("ConfigServerHostedService should start");
    }

    [Fact]
    public async Task FindsConfigServerProviderInPlaceholderProvider()
    {
        var placeholder = new PlaceholderResolverProvider(new List<IConfigurationProvider>
        {
            new ConfigServerConfigurationProvider(new ConfigServerClientSettings
            {
                Enabled = false
            })
        });

        var configurationRoot = new ConfigurationRoot(new List<IConfigurationProvider>
        {
            placeholder
        });

        var service = new ConfigServerHostedService(configurationRoot, null);

        Func<Task> startStopAction = async () =>
        {
            await service.StartAsync(default);
            await service.StopAsync(default);
        };

        await startStopAction.Should().NotThrowAsync("ConfigServerHostedService should start");
    }

    [Fact]
    public async Task ServiceConstructsAndOperatesWithConfigurationManager()
    {
        var configurationManager = new ConfigurationManager();
        configurationManager.AddInMemoryCollection(TestHelpers.FastTestsConfiguration);
        configurationManager.AddConfigServer();
        var service = new ConfigServerHostedService(configurationManager, null);

        Func<Task> startStopAction = async () =>
        {
            await service.StartAsync(default);
            await service.StopAsync(default);
        };

        await startStopAction.Should().NotThrowAsync("ConfigServerHostedService should start");
    }
}
