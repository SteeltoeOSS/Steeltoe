// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.CloudFoundry.ServiceBindings;

namespace Steeltoe.Configuration.CloudFoundry.Test.ServiceBindings;

public sealed class ParentConfigurationTest
{
    [Fact]
    public void Disposing_parent_configuration_does_not_dispose_shared_post_processors()
    {
        var processor = new DisposeCountingPostProcessor();
        var source = new CloudFoundryServiceBindingConfigurationSource(new StringServiceBindingsReader(string.Empty), CloudFoundryServiceBrokerTypes.All);
        source.RegisterPostProcessor(processor);

        var ownerSource = new OwnerSource();

        var builder = new ConfigurationBuilder();
        builder.Add(source);
        builder.Add(ownerSource);

        using (builder.BuildAsRoot())
        {
            using (ConfigurationRoot parentConfigurationRoot = ownerSource.GetParentConfiguration())
            {
                parentConfigurationRoot.Providers.OfType<CloudFoundryServiceBindingConfigurationProvider>().Should().ContainSingle();
            }

            processor.DisposeCount.Should().Be(0);
        }

        processor.DisposeCount.Should().Be(1);
    }

    [Fact]
    public void Building_parent_configuration_does_not_replace_captured_builder_of_copied_sources()
    {
        var otherSource = new OtherSource();
        var ownerSource = new OwnerSource();

        var builder = new ConfigurationBuilder();
        builder.Add(otherSource);
        builder.Add(ownerSource);
        using ConfigurationRoot configurationRoot = builder.BuildAsRoot();

        // Building the parent configuration of the owner builds a copy of the other source, using a temporary builder.
        using ConfigurationRoot ownerParentConfigurationRoot = ownerSource.GetParentConfiguration();
        ownerParentConfigurationRoot.Providers.OfType<OtherProvider>().Should().ContainSingle();

        // The other source must still know about the original builder, which contains the owner source.
        using ConfigurationRoot otherParentConfigurationRoot = otherSource.GetParentConfiguration();
        otherParentConfigurationRoot.Providers.OfType<OwnerProvider>().Should().ContainSingle();
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

    private sealed class OtherSource : PostProcessorConfigurationSource, IConfigurationSource
    {
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            CaptureConfigurationBuilder(builder);
            return new OtherProvider(this);
        }
    }

    private sealed class OtherProvider(PostProcessorConfigurationSource source)
        : PostProcessorConfigurationProvider(source);
}
