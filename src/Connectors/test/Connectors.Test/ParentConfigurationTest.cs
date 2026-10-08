// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
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
}
