// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration;

namespace Steeltoe.Connectors.Test.ParentConfiguration;

/// <summary>
/// Verifies that the host configuration, which HostBuilder chains into the app configuration and hands ownership of, is neither disposed nor impaired
/// when connectors build temporary copies of the parent configuration, including when the placeholder resolver, decryption or Config Server rebuild it.
/// </summary>
public sealed class ChainedHostConfigurationTest
{
    [Fact]
    public void Does_not_dispose_chained_host_configuration_when_disposing_temporary_copy_of_parent_configuration()
    {
        var countingSource = new DisposeCountingConfigurationSource();

        // HostBuilder chains the host configuration into the app configuration, taking ownership of it.
        // Other host builder types are unaffected.
        HostBuilder builder = TestHostBuilderFactory.Create();
        builder.ConfigureHostConfiguration(configurationBuilder => configurationBuilder.Add(countingSource));
        builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddSilentConnector());

        using (builder.Build())
        {
            countingSource.Providers.Should().ContainSingle();
            countingSource.Providers[0].DisposeCount.Should().Be(0);
        }

        countingSource.Providers[0].DisposeCount.Should().Be(1);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void Does_not_dispose_chained_host_configuration_when_composite_source_wraps_it(bool usePlaceholderResolver, bool useDecryption)
    {
        var countingSource = new DisposeCountingConfigurationSource();

        HostBuilder builder = TestHostBuilderFactory.Create();
        builder.ConfigureHostConfiguration(configurationBuilder => configurationBuilder.Add(countingSource));

        // Same order as AddSteeltoe(): the placeholder resolver and decryption move all existing sources, including the chained
        // host configuration, into a composite source that builds them.
        builder.ConfigureAppConfiguration(configurationBuilder =>
        {
            configurationBuilder.SetRecursionGuard();
            configurationBuilder.AddSilentConnector();
            configurationBuilder.AddCompositeSources(usePlaceholderResolver, useDecryption);
        });

        using (builder.Build())
        {
            countingSource.Providers.Should().ContainSingle();
            countingSource.Providers[0].DisposeCount.Should().Be(0);
        }

        countingSource.Providers[0].DisposeCount.Should().Be(1);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void Restores_chained_host_configuration_ownership_after_building_parent_configuration(bool usePlaceholderResolver, bool useDecryption)
    {
        var countingSource = new DisposeCountingConfigurationSource();

        HostBuilder builder = TestHostBuilderFactory.Create();
        builder.ConfigureHostConfiguration(configurationBuilder => configurationBuilder.Add(countingSource));

        ChainedConfigurationSource[] chainedSources = [];

        builder.ConfigureAppConfiguration(configurationBuilder =>
        {
            configurationBuilder.SetRecursionGuard();
            configurationBuilder.AddSilentConnector();
            configurationBuilder.AddCompositeSources(usePlaceholderResolver, useDecryption);

            chainedSources = [.. configurationBuilder.EnumerateSources<ChainedConfigurationSource>()];
        });

        using (builder.Build())
        {
            chainedSources.Should().ContainSingle().Which.ShouldDisposeConfiguration.Should().BeTrue();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Does_not_dispose_chained_host_configuration_when_connector_builds_parent_configuration_containing_Config_Server(bool addConfigServerFirst)
    {
        var hostSource = new DisposeCountingConfigurationSource();

        HostBuilder builder = TestHostBuilderFactory.Create();
        builder.ConfigureHostConfiguration(configurationBuilder => configurationBuilder.Add(hostSource));

        builder.ConfigureAppConfiguration(configurationBuilder =>
        {
            configurationBuilder.SetRecursionGuard();

            // Building the parent configuration of the connector rebuilds Config Server, which builds a nested configuration from its sources,
            // including the chained host configuration.
            if (addConfigServerFirst)
            {
                configurationBuilder.AddSilentConfigServer();
                configurationBuilder.AddSilentConnector();
            }
            else
            {
                configurationBuilder.AddSilentConnector();
                configurationBuilder.AddSilentConfigServer();
            }
        });

        using (builder.Build())
        {
            hostSource.Providers.Should().ContainSingle();
            hostSource.Providers[0].DisposeCount.Should().Be(0);
        }

        hostSource.Providers[0].DisposeCount.Should().Be(1);
    }

    [Fact]
    public void Host_configuration_keeps_reloading_after_connector_builds_parent_configuration_containing_Config_Server()
    {
        var fileProvider = new MemoryFileProvider();
        fileProvider.IncludeAppSettingsJsonFile("""{ "Example": { "Value": "initial" } }""");

        HostBuilder builder = TestHostBuilderFactory.Create();
        builder.ConfigureHostConfiguration(configurationBuilder => configurationBuilder.AddInMemoryAppSettingsJsonFile(fileProvider));

        builder.ConfigureAppConfiguration(configurationBuilder =>
        {
            configurationBuilder.SetRecursionGuard();
            configurationBuilder.AddSilentConfigServer();
            configurationBuilder.AddSilentConnector();
        });

        using IHost host = builder.Build();
        var configuration = host.Services.GetRequiredService<IConfiguration>();

        configuration["Example:Value"].Should().Be("initial");

        fileProvider.ReplaceAppSettingsJsonFile("""{ "Example": { "Value": "changed" } }""");
        fileProvider.NotifyChanged();

        configuration["Example:Value"].Should().Be("changed");
    }
}
