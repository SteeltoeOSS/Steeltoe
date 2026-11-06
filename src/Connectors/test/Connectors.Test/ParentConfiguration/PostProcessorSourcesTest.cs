// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.TestResources;
using Steeltoe.Common.TestResources.IO;
using Steeltoe.Configuration;
using Steeltoe.Configuration.CloudFoundry.ServiceBindings;
using Steeltoe.Configuration.Kubernetes.ServiceBindings;

namespace Steeltoe.Connectors.Test.ParentConfiguration;

/// <summary>
/// Verifies how the post-processor sources (connectors, and the Cloud Foundry and Kubernetes service bindings) behave when they are copied into each
/// other's temporary copy of the parent configuration: shared post-processors are not disposed together with the copy, and the copy does not replace the
/// builder that a source captured.
/// </summary>
public sealed class PostProcessorSourcesTest
{
    [Fact]
    public void Disposing_temporary_copy_of_parent_configuration_does_not_dispose_post_processors_of_Cloud_Foundry_service_bindings()
    {
        var processor = new DisposeCountingPostProcessor();

        var bindingsSource =
            new CloudFoundryServiceBindingConfigurationSource(new StringServiceBindingsReader(string.Empty), CloudFoundryServiceBrokerTypes.All);

        bindingsSource.RegisterPostProcessor(processor);

        VerifyPostProcessorsOutliveTemporaryCopy<CloudFoundryServiceBindingConfigurationProvider>(bindingsSource, processor);
    }

    [Fact]
    public void Disposing_temporary_copy_of_parent_configuration_does_not_dispose_post_processors_of_Kubernetes_service_bindings()
    {
        using var sandbox = new Sandbox();

        var processor = new DisposeCountingPostProcessor();
        var bindingsSource = new KubernetesServiceBindingConfigurationSource(new DirectoryServiceBindingsReader(sandbox.FullPath));
        bindingsSource.RegisterPostProcessor(processor);

        VerifyPostProcessorsOutliveTemporaryCopy<KubernetesServiceBindingConfigurationProvider>(bindingsSource, processor);
    }

    [Fact]
    public void Disposing_temporary_copy_of_parent_configuration_of_service_bindings_does_not_dispose_post_processors_of_connector()
    {
        var processor = new DisposeCountingPostProcessor();
        var connectorSource = new ConnectionStringPostProcessorConfigurationSource(false);
        connectorSource.RegisterPostProcessor(processor);

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundryServiceBindings(new StringServiceBindingsReader(string.Empty));
        builder.Add(connectorSource);

        using (builder.BuildAsRoot())
        {
            CloudFoundryServiceBindingConfigurationSource bindingsSource = builder.EnumerateSources<CloudFoundryServiceBindingConfigurationSource>().Single();

            // The service bindings source builds its parent configuration, which contains a copy of the connector source.
            using (ConfigurationRoot parentConfigurationRoot = bindingsSource.GetParentConfiguration())
            {
                parentConfigurationRoot.Providers.OfType<ConnectionStringPostProcessorConfigurationProvider>().Should().ContainSingle();
            }

            processor.DisposeCount.Should().Be(0);
        }

        processor.DisposeCount.Should().Be(1);
    }

    [Fact]
    public void Building_parent_configuration_does_not_replace_captured_builder_of_copied_sources()
    {
        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundryServiceBindings(new StringServiceBindingsReader(string.Empty));
        builder.AddSilentConnector();

        using ConfigurationRoot configurationRoot = builder.BuildAsRoot();

        CloudFoundryServiceBindingConfigurationSource bindingsSource = builder.EnumerateSources<CloudFoundryServiceBindingConfigurationSource>().Single();

        ConnectionStringPostProcessorConfigurationSource connectorSource =
            builder.EnumerateSources<ConnectionStringPostProcessorConfigurationSource>().Single();

        // Building the parent configuration of the connector builds a copy of the service bindings source, using a temporary builder.
        using ConfigurationRoot connectorParentConfigurationRoot = connectorSource.GetParentConfiguration();
        connectorParentConfigurationRoot.Providers.OfType<CloudFoundryServiceBindingConfigurationProvider>().Should().ContainSingle();

        // The service bindings source must still know about the original builder, which contains the connector source.
        using ConfigurationRoot bindingsParentConfigurationRoot = bindingsSource.GetParentConfiguration();
        bindingsParentConfigurationRoot.Providers.OfType<ConnectionStringPostProcessorConfigurationProvider>().Should().ContainSingle();
    }

    /// <summary>
    /// Builds a configuration with the specified service bindings source (which has the processor registered) and a connector, which builds a temporary copy
    /// of the parent configuration that contains another provider of that service bindings source. Verifies that disposing the copy leaves the processor
    /// alone, because it is shared with the provider of the actual configuration.
    /// </summary>
    private static void VerifyPostProcessorsOutliveTemporaryCopy<TBindingsProvider>(IConfigurationSource bindingsSource, DisposeCountingPostProcessor processor)
        where TBindingsProvider : IConfigurationProvider
    {
        var countingSource = new DisposeCountingConfigurationSource();

        var builder = new ConfigurationBuilder();
        builder.Add(countingSource);
        builder.Add(bindingsSource);
        builder.AddSilentConnector();

        using (ConfigurationRoot configurationRoot = builder.BuildAsRoot())
        {
            // One provider for the actual configuration, and one for the temporary copy that the connector has already disposed.
            countingSource.Providers.Should().HaveCount(2);
            countingSource.Providers[1].DisposeCount.Should().Be(1);

            configurationRoot.Providers.OfType<TBindingsProvider>().Should().ContainSingle();
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
}
