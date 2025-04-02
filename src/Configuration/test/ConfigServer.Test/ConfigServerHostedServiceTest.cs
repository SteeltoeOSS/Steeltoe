// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.Encryption;
using Steeltoe.Configuration.Placeholder;

namespace Steeltoe.Configuration.ConfigServer.Test;

public sealed class ConfigServerHostedServiceTest
{
    [Fact]
    public async Task ServiceConstructsAndOperatesWithConfigurationRoot()
    {
        var provider = new ConfigServerConfigurationProvider(new ConfigServerClientOptions
        {
            Enabled = false
        }, null, null, NullLoggerFactory.Instance);

        var configurationRoot = new ConfigurationRoot([provider]);
        var service = new ConfigServerHostedService(configurationRoot, []);

        Func<Task> startStopAction = async () =>
        {
            await service.StartAsync(TestContext.Current.CancellationToken);
            await service.StopAsync(TestContext.Current.CancellationToken);
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
            await service.StartAsync(TestContext.Current.CancellationToken);
            await service.StopAsync(TestContext.Current.CancellationToken);
        };

        await startStopAction.Should().NotThrowAsync("ConfigServerHostedService should start");
    }

    [Fact]
    public void ThrowsWhenConfigServerProviderNotFound()
    {
        var builder = new ConfigurationBuilder();
        IConfigurationRoot configurationRoot = builder.Build();

        Action action = () => _ = new ConfigServerHostedService(configurationRoot, []);

        action.Should().ThrowExactly<InvalidOperationException>().WithMessage("ConfigServerConfigurationProvider was not found in configuration.");
    }

    [Fact]
    public void FindsConfigServerProviderInPlaceholderProviderInDecryptionProvider()
    {
        var builder = new ConfigurationBuilder();
        builder.AddConfigServer();
        builder.AddPlaceholderResolver();
        builder.AddDecryption();
        IConfigurationRoot configurationRoot = builder.Build();

        Action action = () => _ = new ConfigServerHostedService(configurationRoot, []);

        action.Should().NotThrow();
    }
}
