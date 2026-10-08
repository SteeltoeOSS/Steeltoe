// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Configuration.Kubernetes.ServiceBindings.Test;

public sealed class ParentConfigurationTest
{
    [Fact]
    public void Disposing_parent_configuration_does_not_dispose_shared_post_processors()
    {
        var processor = new DisposeCountingPostProcessor();
        string rootDirectory = Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "resources", "k8s");
        var source = new KubernetesServiceBindingConfigurationSource(new DirectoryServiceBindingsReader(rootDirectory));
        source.RegisterPostProcessor(processor);

        var ownerSource = new OwnerSource();

        var builder = new ConfigurationBuilder();
        builder.Add(source);
        builder.Add(ownerSource);

        using (builder.BuildAsRoot())
        {
            using (ConfigurationRoot parentConfigurationRoot = ownerSource.GetParentConfiguration())
            {
                parentConfigurationRoot.Providers.OfType<KubernetesServiceBindingConfigurationProvider>().Should().ContainSingle();
            }

            processor.DisposeCount.Should().Be(0);
        }

        processor.DisposeCount.Should().Be(1);
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
