// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.Encryption;
using Steeltoe.Configuration.Placeholder;

namespace Steeltoe.Configuration.ConfigServer.Test;

public sealed class ConfigServerServiceCollectionExtensionsTest
{
    [Fact]
    public void DoesNotAddConfigServerMultipleTimes()
    {
        var builder = new ConfigurationBuilder();
        builder.AddConfigServer();
        builder.AddPlaceholderResolver();
        builder.AddDecryption();
        builder.AddConfigServer();
        builder.AddPlaceholderResolver();
        builder.AddDecryption();
        builder.AddConfigServer();

        builder.EnumerateSources<ConfigServerConfigurationSource>().Should().ContainSingle();

        using ConfigurationRoot configurationRoot = builder.BuildAsRoot();

        configurationRoot.EnumerateProviders<ConfigServerConfigurationProvider>().Should().ContainSingle();
    }

    [Fact]
    public async Task AddConfigServerServices_registers_IConfigurationRoot_for_backward_compatibility_only()
    {
        var builder = new ConfigurationBuilder();
        builder.Add(FastTestConfigurations.ConfigServer);
        builder.AddConfigServer();
        ConfigurationRoot configurationRoot = builder.BuildAsRoot();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(_ => configurationRoot);
        services.AddConfigServerServices();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        serviceProvider.GetService<IConfigurationRoot>().Should().NotBeNull();
    }
}
