// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration;
using Steeltoe.Connectors.PostgreSql;

namespace Steeltoe.Connectors.Test;

public sealed class ParentConfigurationTest
{
    [Fact]
    public void Disposes_temporary_copy_of_parent_configuration()
    {
        var countingSource = new DisposeCountingConfigurationSource();

        var builder = new ConfigurationBuilder();
        builder.Add(countingSource);
        builder.ConfigurePostgreSql(options => options.SkipDefaultServiceBindings = true);

        using (builder.BuildAsRoot())
        {
            // One provider for the actual configuration, and one for the temporary copy used to post-process connection strings.
            countingSource.Providers.Should().HaveCount(2);
            countingSource.Providers[0].DisposeCount.Should().Be(0);
            countingSource.Providers[1].DisposeCount.Should().Be(1);
        }

        countingSource.Providers.Should().OnlyContain(provider => provider.DisposeCount == 1);
    }

    [Fact]
    public void Disposes_previous_temporary_copy_of_parent_configuration_when_configuration_changes()
    {
        var countingSource = new DisposeCountingConfigurationSource();

        var builder = new ConfigurationBuilder();
        builder.Add(countingSource);

        builder.ConfigurePostgreSql(options =>
        {
            options.SkipDefaultServiceBindings = true;
            options.DetectConfigurationChanges = true;
        });

        using (builder.BuildAsRoot())
        {
            // Only the actual provider and the copy of the parent configuration whose changes are being observed remain alive.
            countingSource.Providers.Count(provider => provider.DisposeCount == 0).Should().Be(2);

            for (int index = 0; index < 5; index++)
            {
                DisposeCountingConfigurationProvider lastProvider = countingSource.Providers.Last(provider => provider.DisposeCount == 0);

                lastProvider.TriggerReload();

                lastProvider.DisposeCount.Should().Be(1);
                countingSource.Providers.Count(provider => provider.DisposeCount == 0).Should().Be(2);
            }
        }

        countingSource.Providers.Should().OnlyContain(provider => provider.DisposeCount == 1);
    }

    [Fact]
    public void Does_not_accumulate_temporary_copies_of_parent_configuration_when_file_changes()
    {
        var fileProvider = new MemoryFileProvider();

        fileProvider.IncludeAppSettingsJsonFile("""
            {
              "Example": {
                "Value": "initial"
              }
            }
            """);

        var countingSource = new DisposeCountingConfigurationSource();

        using (var manager = new ConfigurationManager())
        {
            manager.AddInMemoryAppSettingsJsonFile(fileProvider);
            manager.Sources.Add(countingSource);

            manager.ConfigurePostgreSql(options =>
            {
                options.SkipDefaultServiceBindings = true;
                options.DetectConfigurationChanges = true;
            });

            for (int index = 0; index < 25; index++)
            {
                fileProvider.ReplaceAppSettingsJsonFile($$"""
                    {
                      "Example": {
                        "Value": "changed-{{index}}"
                      }
                    }
                    """);

                fileProvider.NotifyChanged();

                // The actual provider, and the copy of the parent configuration that is being observed for changes.
                countingSource.Providers.Count(provider => provider.DisposeCount == 0).Should().Be(2);
            }

            // Proves the changes were detected: each reload creates new copies of the parent configuration.
            countingSource.Providers.Should().HaveCountGreaterThan(25);
        }

        countingSource.Providers.Should().OnlyContain(provider => provider.DisposeCount == 1);
    }

    [Fact]
    public void Connector_source_does_not_dispose_shared_post_processors_when_copied_to_parent_configuration()
    {
        var processor = new DisposeCountingPostProcessor();
        var connectorSource = new ConnectionStringPostProcessorConfigurationSource(false);
        connectorSource.RegisterPostProcessor(processor);

        var ownerSource = new OwnerSource();

        var builder = new ConfigurationBuilder();
        builder.Add(connectorSource);
        builder.Add(ownerSource);

        using (builder.BuildAsRoot())
        {
            using (ConfigurationRoot parentConfigurationRoot = ownerSource.GetParentConfiguration())
            {
                parentConfigurationRoot.Providers.OfType<ConnectionStringPostProcessorConfigurationProvider>().Should().ContainSingle();
            }

            processor.DisposeCount.Should().Be(0);
        }

        processor.DisposeCount.Should().Be(1);
    }

    [Fact]
    public void Does_not_dispose_chained_host_configuration_when_disposing_temporary_copy_of_parent_configuration()
    {
        var countingSource = new DisposeCountingConfigurationSource();

        // HostBuilder chains the host configuration into the app configuration, taking ownership of it.
        // Other host builder types are unaffected.
        HostBuilder builder = TestHostBuilderFactory.Create();
        builder.ConfigureHostConfiguration(configurationBuilder => configurationBuilder.Add(countingSource));

        builder.ConfigureAppConfiguration(configurationBuilder =>
            configurationBuilder.ConfigurePostgreSql(options => options.SkipDefaultServiceBindings = true));

        using IHost host = builder.Build();

        countingSource.Providers.Should().ContainSingle();
        countingSource.Providers[0].DisposeCount.Should().Be(0);
    }

    [Fact]
    public void Builds_parent_configuration_when_connector_source_is_wrapped_in_composite_source()
    {
        var countingSource = new DisposeCountingConfigurationSource();

        var builder = new ConfigurationBuilder();
        builder.Add(countingSource);
        builder.ConfigurePostgreSql(options => options.SkipDefaultServiceBindings = true);

        // Does what AddPlaceholderResolver() and AddDecryption() do: move all existing sources into a composite source that builds them.
        var compositeSource = new TestCompositeConfigurationSource();

        foreach (IConfigurationSource source in builder.Sources)
        {
            compositeSource.Sources.Add(source);
        }

        builder.Sources.Clear();
        builder.Sources.Add(compositeSource);

        using (builder.BuildAsRoot())
        {
            // One build for the actual configuration, and one for the temporary copy of the parent configuration used to post-process connection strings.
            compositeSource.BuildCount.Should().Be(2);

            // The temporary copy still contains the sources that surround the connector source.
            countingSource.Providers.Should().HaveCount(2);
        }

        countingSource.Providers.Should().OnlyContain(provider => provider.DisposeCount == 1);
    }

    private sealed class DisposeCountingConfigurationSource : IConfigurationSource
    {
        public List<DisposeCountingConfigurationProvider> Providers { get; } = [];

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            var provider = new DisposeCountingConfigurationProvider();
            Providers.Add(provider);
            return provider;
        }
    }

    private sealed class DisposeCountingConfigurationProvider : ConfigurationProvider, IDisposable
    {
        private long _disposeCount;

        public long DisposeCount => Interlocked.Read(ref _disposeCount);

        public void TriggerReload()
        {
            OnReload();
        }

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

    private sealed class OwnerSource : PostProcessorConfigurationSource, IConfigurationSource
    {
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            CaptureConfigurationBuilder(builder);
            return new OwnerProvider(this);
        }
    }

    private sealed class OwnerProvider(PostProcessorConfigurationSource source)
        : PostProcessorConfigurationProvider(source);

    private sealed class TestCompositeConfigurationSource : ICompositeConfigurationSource
    {
        private const int MaxBuildCount = 10;

        public IList<IConfigurationSource> Sources { get; } = [];
        public int BuildCount { get; private set; }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            BuildCount++;

            // Fail instead of overflowing the stack when the connector source keeps building its own parent configuration.
            if (BuildCount > MaxBuildCount)
            {
                throw new InvalidOperationException("Infinite recursion detected.");
            }

            List<IConfigurationProvider> providers = [.. Sources.Select(source => source.Build(builder))];
            return new TestCompositeConfigurationProvider(providers);
        }
    }

    private sealed class TestCompositeConfigurationProvider(List<IConfigurationProvider> providers) : ConfigurationProvider, IDisposable
    {
        public override void Load()
        {
            foreach (IConfigurationProvider provider in providers)
            {
                provider.Load();
            }
        }

        public void Dispose()
        {
            foreach (IDisposable disposable in providers.OfType<IDisposable>())
            {
                disposable.Dispose();
            }
        }
    }
}
