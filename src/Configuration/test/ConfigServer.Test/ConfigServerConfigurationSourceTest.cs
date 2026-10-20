// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.TestResources;

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

    [Fact]
    public void Config_Server_provider_does_not_dispose_post_processors_that_are_shared_with_the_actual_configuration()
    {
        var processor = new DisposeCountingPostProcessor();

        // Like the Cloud Foundry and Kubernetes service binding sources, which are also built in the nested configuration.
        var postProcessorSource = new TestPostProcessorConfigurationSource
        {
            SkipsOwnParentConfiguration = false
        };

        postProcessorSource.RegisterPostProcessor(processor);

        var builder = new ConfigurationBuilder();
        builder.Add(postProcessorSource);

        builder.AddConfigServer(new ConfigServerClientOptions
        {
            Enabled = false
        });

        using (builder.BuildAsRoot())
        {
            // One provider for the actual configuration, and one in the nested configuration that Config Server builds from the same sources.
            postProcessorSource.ProvidersCreated.Should().HaveCount(2);
            postProcessorSource.ProvidersCreated.Count(provider => provider.DisposesPostProcessors).Should().Be(1);
        }

        // Both providers have been disposed, but only the one in the actual configuration is responsible for disposing the shared post-processor.
        processor.DisposeCount.Should().Be(1);
    }

    [Fact]
    public void Config_Server_nested_configuration_does_not_replace_captured_parent_configuration()
    {
        var postProcessorSource = new TestPostProcessorConfigurationSource();

        var builder = new ConfigurationBuilder();
        builder.Add(postProcessorSource);

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
            using ConfigurationRoot parentConfigurationRoot = postProcessorSource.GetParentConfiguration();

            parentConfigurationRoot["AddedAfterConfigServer"].Should().Be("value");
        }
    }

    [Fact]
    public void Post_processor_source_is_skipped_in_Config_Server_nested_configuration_to_prevent_infinite_recursion()
    {
        var postProcessorSource = new TestPostProcessorConfigurationSource
        {
            LoadBuildsParentConfiguration = true
        };

        var builder = new ConfigurationBuilder();
        builder.Add(postProcessorSource);

        builder.AddConfigServer(new ConfigServerClientOptions
        {
            Enabled = false
        });

        using (builder.BuildAsRoot())
        {
            // Only the actual configuration contains a provider. Loading it builds the parent configuration, which contains Config Server, which builds
            // a nested configuration from the same sources. Without skipping the source there, this would never end.
            postProcessorSource.ProvidersCreated.Should().ContainSingle();
        }
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

    /// <summary>
    /// Behaves like the connector source (or, when <see cref="SkipsOwnParentConfiguration" /> is turned off, like the service binding sources): captures the
    /// builder, optionally skips itself in the temporary parent configuration, and optionally builds the parent configuration when loading, like connectors
    /// do to post-process connection strings.
    /// </summary>
    private sealed class TestPostProcessorConfigurationSource : PostProcessorConfigurationSource, IConfigurationSource
    {
        private const int MaxProvidersCreated = 10;

        public List<TestPostProcessorConfigurationProvider> ProvidersCreated { get; } = [];
        public bool LoadBuildsParentConfiguration { get; init; }
        public bool SkipsOwnParentConfiguration { get; init; } = true;

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            if (SkipsOwnParentConfiguration && IsBuildingOwnParentConfiguration(builder))
            {
                return new MemoryConfigurationProvider(new MemoryConfigurationSource());
            }

            CaptureConfigurationBuilder(builder);

            // Fail instead of overflowing the stack when the source keeps building its own parent configuration.
            if (ProvidersCreated.Count >= MaxProvidersCreated)
            {
                throw new InvalidOperationException("Infinite recursion detected.");
            }

            var provider = new TestPostProcessorConfigurationProvider(this)
            {
                DisposesPostProcessors = !IsBuildingParentConfiguration(builder)
            };

            ProvidersCreated.Add(provider);
            return provider;
        }
    }

    private sealed class TestPostProcessorConfigurationProvider(TestPostProcessorConfigurationSource source)
        : PostProcessorConfigurationProvider(source)
    {
        private readonly TestPostProcessorConfigurationSource _source = source;

        public override void Load()
        {
            if (_source.LoadBuildsParentConfiguration)
            {
                using ConfigurationRoot parentConfigurationRoot = _source.GetParentConfiguration();
                PostProcessConfiguration();
            }
        }
    }
}
