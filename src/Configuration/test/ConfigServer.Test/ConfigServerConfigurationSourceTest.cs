// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Logging.Abstractions;

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
        innerSource.ProvidersCreated.Should().HaveCount(2);
        innerSource.ProvidersCreated.Should().OnlyContain(provider => provider.DisposeCount == 0);

        manager.Dispose();

        innerSource.ProvidersCreated.Should().OnlyContain(provider => provider.DisposeCount == 1);
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

            innerSource.ProvidersCreated.Should().HaveCount(2);

            // Removing a source causes ConfigurationManager to dispose all providers and build them again.
            // This happens when adding the placeholder resolver after calling AddConfigServer.
            manager.Sources.Remove(otherSource);

            innerSource.ProvidersCreated.Should().HaveCount(4);
            innerSource.ProvidersCreated.Take(2).Should().OnlyContain(provider => provider.DisposeCount == 1);
            innerSource.ProvidersCreated.Skip(2).Should().OnlyContain(provider => provider.DisposeCount == 0);
        }

        innerSource.ProvidersCreated.Should().OnlyContain(provider => provider.DisposeCount == 1);
    }

    private static void AddConfigurationSource(IConfigurationBuilder builder, IConfigurationSource source)
    {
        builder.Add(source);
    }

    private sealed class DisposeCountingConfigurationSource : IConfigurationSource
    {
        public List<DisposeCountingConfigurationProvider> ProvidersCreated { get; } = [];

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            var provider = new DisposeCountingConfigurationProvider();
            ProvidersCreated.Add(provider);
            return provider;
        }
    }

    private sealed class DisposeCountingConfigurationProvider : ConfigurationProvider, IDisposable
    {
        private long _disposeCount;

        public long DisposeCount => Interlocked.Read(ref _disposeCount);

        public void Dispose()
        {
            Interlocked.Increment(ref _disposeCount);
        }
    }
}
