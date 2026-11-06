// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.CloudFoundry.ServiceBindings;

namespace Steeltoe.Configuration.ConfigServer.Test;

public sealed class ConfigServerConfigurationSourceTest
{
    [Fact]
    public void Constructors_InitializesProperties()
    {
        var options = new ConfigServerClientOptions();
        var memSource = new MemoryConfigurationSource();

        List<IConfigurationSource> sources = [memSource];

        var source = new ConfigServerConfigurationSource(options, sources, new Dictionary<string, object>
        {
            ["foo"] = "bar"
        }, null, null, NullLoggerFactory.Instance);

        source.DefaultOptions.Should().Be(options);
        source.Configuration.Should().BeNull();
        source.Sources.Should().NotBeSameAs(sources);
        source.Sources.Should().ContainSingle().Which.Should().Be(memSource);
        source.Properties.Should().ContainSingle();
        source.Properties.Should().ContainKey("foo").WhoseValue.Should().Be("bar");
    }

    [Fact]
    public void Build_ReturnsProvider()
    {
        var options = new ConfigServerClientOptions();
        var memSource = new MemoryConfigurationSource();
        List<IConfigurationSource> sources = [memSource];

        var source = new ConfigServerConfigurationSource(options, sources, null, null, null, NullLoggerFactory.Instance);
        IConfigurationProvider provider = source.Build(new ConfigurationBuilder());

        provider.Should().BeOfType<ConfigServerConfigurationProvider>();

        ((IDisposable)provider).Dispose();
    }

    [Fact]
    public void Disposing_configuration_manager_disposes_providers_created_by_Config_Server_source()
    {
        var innerSource = new DisposeCountingConfigurationSource();

        var manager = new ConfigurationManager();
        AddConfigurationSource(manager, innerSource);

        manager.AddConfigServer(new ConfigServerClientOptions
        {
            Enabled = false
        });

        // One provider for the application configuration, and one created by the Config Server source for its own configuration.
        innerSource.Providers.Should().HaveCount(2);
        innerSource.Providers.Should().OnlyContain(provider => provider.DisposeCount == 0);

        manager.Dispose();

        innerSource.Providers.Should().OnlyContain(provider => provider.DisposeCount == 1);
    }

    [Fact]
    public void Rebuilding_sources_disposes_providers_created_by_previous_Config_Server_source()
    {
        var innerSource = new DisposeCountingConfigurationSource();
        var otherSource = new MemoryConfigurationSource();

        using (var manager = new ConfigurationManager())
        {
            AddConfigurationSource(manager, innerSource);
            AddConfigurationSource(manager, otherSource);

            manager.AddConfigServer(new ConfigServerClientOptions
            {
                Enabled = false
            });

            innerSource.Providers.Should().HaveCount(2);

            // Removing a source causes ConfigurationManager to dispose all providers and build them again.
            // This happens when adding the placeholder resolver after calling AddConfigServer.
            manager.Sources.Remove(otherSource);

            innerSource.Providers.Should().HaveCount(4);
            innerSource.Providers.Take(2).Should().OnlyContain(provider => provider.DisposeCount == 1);
            innerSource.Providers.Skip(2).Should().OnlyContain(provider => provider.DisposeCount == 0);
        }

        innerSource.Providers.Should().OnlyContain(provider => provider.DisposeCount == 1);
    }

    [Fact]
    public void Config_Server_provider_does_not_dispose_post_processors_that_are_shared_with_the_actual_configuration()
    {
        var processor = new DisposeCountingPostProcessor();
        var countingSource = new DisposeCountingConfigurationSource();

        // The service binding sources are also built in the nested configuration that Config Server creates from the same sources.
        var bindingsSource =
            new CloudFoundryServiceBindingConfigurationSource(new StringServiceBindingsReader(string.Empty), CloudFoundryServiceBrokerTypes.All);

        bindingsSource.RegisterPostProcessor(processor);

        var builder = new ConfigurationBuilder();
        builder.Add(countingSource);
        builder.Add(bindingsSource);

        builder.AddConfigServer(new ConfigServerClientOptions
        {
            Enabled = false
        });

        using (ConfigurationRoot configurationRoot = builder.BuildAsRoot())
        {
            // One provider for the actual configuration, and one in the nested configuration that Config Server builds from the same sources.
            countingSource.Providers.Should().HaveCount(2);
            configurationRoot.Providers.OfType<CloudFoundryServiceBindingConfigurationProvider>().Should().ContainSingle();
            processor.DisposeCount.Should().Be(0);
        }

        // Both providers have been disposed, but only the one in the actual configuration is responsible for disposing the shared post-processor.
        processor.DisposeCount.Should().Be(1);
    }

    [Fact]
    public void Config_Server_nested_configuration_does_not_replace_captured_parent_configuration()
    {
        var bindingsSource =
            new CloudFoundryServiceBindingConfigurationSource(new StringServiceBindingsReader(string.Empty), CloudFoundryServiceBrokerTypes.All);

        var builder = new ConfigurationBuilder();
        builder.Add(bindingsSource);

        builder.AddConfigServer(new ConfigServerClientOptions
        {
            Enabled = false
        });

        builder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AddedAfterConfigServer"] = "value"
        });

        using (builder.BuildAsRoot())
        {
            using ConfigurationRoot parentConfigurationRoot = bindingsSource.GetParentConfiguration();

            parentConfigurationRoot["AddedAfterConfigServer"].Should().Be("value");
        }
    }

    private static void AddConfigurationSource(IConfigurationBuilder builder, IConfigurationSource source)
    {
        builder.Add(source);
    }

    private sealed class DisposeCountingPostProcessor : IConfigurationPostProcessor, IDisposable
    {
        private long _disposeCount;

        public long DisposeCount => Interlocked.Read(ref _disposeCount);

        public void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string?> configurationData)
        {
        }

        public void Dispose()
        {
            Interlocked.Increment(ref _disposeCount);
        }
    }
}
