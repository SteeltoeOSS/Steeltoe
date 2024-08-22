// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.Placeholder;

namespace Steeltoe.Configuration.ConfigServer.Test;

public sealed class ConfigServerHostedServiceTest
{
    [Fact]
    public async Task ServiceConstructsAndOperatesWithConfigurationRoot()
    {
        var configurationRoot = new ConfigurationRoot(new List<IConfigurationProvider>
        {
            new ConfigServerConfigurationProvider(new ConfigServerClientOptions
            {
                Enabled = false
            }, null, null, NullLoggerFactory.Instance)
        });

        var service = new ConfigServerHostedService(configurationRoot, []);

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
            new ConfigServerConfigurationProvider(new ConfigServerClientOptions
            {
                Enabled = false
            }, null, null, NullLoggerFactory.Instance)
        }, NullLoggerFactory.Instance);

        var configurationRoot = new ConfigurationRoot(new List<IConfigurationProvider>
        {
            placeholder
        });

        var service = new ConfigServerHostedService(configurationRoot, []);

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
        configurationManager.Add(FastTestConfigurations.ConfigServer);
        configurationManager.AddConfigServer();
        var service = new ConfigServerHostedService(configurationManager, []);

        Func<Task> startStopAction = async () =>
        {
            await service.StartAsync(default);
            await service.StopAsync(default);
        };

        await startStopAction.Should().NotThrowAsync("ConfigServerHostedService should start");
    }
}
