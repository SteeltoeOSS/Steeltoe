// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common.TestResources;
using Xunit;

namespace Steeltoe.Configuration.Kubernetes.ServiceBinding.Test;

public sealed class KubernetesServiceBindingConfigurationProviderTest
{
    [Fact]
    public void EnvironmentVariableNotSet()
    {
        using var scope = new EnvironmentVariableScope(EnvironmentServiceBindingsReader.EnvironmentVariableName, null);

        // Optional defaults true, no throw
        var source = new KubernetesServiceBindingConfigurationSource(new EnvironmentServiceBindingsReader());
        var provider = new KubernetesServiceBindingConfigurationProvider(source);

        provider.Load();

        // Optional, no throw
        source = new KubernetesServiceBindingConfigurationSource(new EnvironmentServiceBindingsReader())
        {
            Optional = false
        };

        // Not optional, should throw
        provider = new KubernetesServiceBindingConfigurationProvider(source);
        Action action = () => provider.Load();
        action.Should().ThrowExactly<DirectoryNotFoundException>();
    }

    [Fact]
    public void EnvironmentVariableSet_InvalidDirectory()
    {
        string rootDirectory = GetK8SResourcesDirectory("invalid");
        using var scope = new EnvironmentVariableScope(EnvironmentServiceBindingsReader.EnvironmentVariableName, rootDirectory);

        // Not optional, should throw
        var source = new KubernetesServiceBindingConfigurationSource(new EnvironmentServiceBindingsReader());
        var provider = new KubernetesServiceBindingConfigurationProvider(source);
        provider.Load();

        // Optional, no throw
        source = new KubernetesServiceBindingConfigurationSource(new EnvironmentServiceBindingsReader())
        {
            Optional = false
        };

        provider = new KubernetesServiceBindingConfigurationProvider(source);
        Action action = () => provider.Load();
        action.Should().ThrowExactly<DirectoryNotFoundException>();
    }

    [Fact]
    public void EnvironmentVariableSet_ValidDirectory()
    {
        string rootDirectory = GetK8SResourcesDirectory(string.Empty);
        using var scope = new EnvironmentVariableScope(EnvironmentServiceBindingsReader.EnvironmentVariableName, rootDirectory);

        var source = new KubernetesServiceBindingConfigurationSource(new EnvironmentServiceBindingsReader());
        var provider = new KubernetesServiceBindingConfigurationProvider(source);
        provider.Load();

        provider.TryGet("k8s:bindings:test-name-1:type", out string? value).Should().BeTrue();
        value.Should().Be("test-type-1");
        provider.TryGet("k8s:bindings:test-name-1:provider", out value).Should().BeTrue();
        value.Should().Be("test-provider-1");
        provider.TryGet("k8s:bindings:test-name-1:test-secret-key", out value).Should().BeTrue();
        value.Should().Be("test-secret-value");

        provider.TryGet("k8s:bindings:test-name-2:type", out value).Should().BeTrue();
        value.Should().Be("test-type-2");
        provider.TryGet("k8s:bindings:test-name-2:provider", out value).Should().BeTrue();
        value.Should().Be("test-provider-2");
        provider.TryGet("k8s:bindings:test-name-2:test-secret-key", out value).Should().BeTrue();
        value.Should().Be("test-secret-value");

        provider.TryGet("k8s:bindings:test-k8s:type", out value).Should().BeTrue();
        value.Should().Be("test-type-1");
        provider.TryGet("k8s:bindings:test-k8s:provider", out value).Should().BeTrue();
        value.Should().Be("test-provider-1");
        provider.TryGet("k8s:bindings:test-k8s:test-secret-key", out value).Should().BeTrue();
        value.Should().Be("test-secret-value");
    }

    [Fact]
    public void NoBindings_DoesNotThrow()
    {
        var builder = new ConfigurationBuilder();
        builder.Add(new KubernetesServiceBindingConfigurationSource(new DirectoryServiceBindingsReader(GetEmptyK8SResourcesDirectory())));

        Action action = () => builder.Build();
        action.Should().NotThrow();
    }

    [Fact]
    public void PostProcessors_OnByDefault()
    {
        string rootDirectory = GetK8SResourcesDirectory(string.Empty);

        var source = new KubernetesServiceBindingConfigurationSource(new DirectoryServiceBindingsReader(rootDirectory));
        var postProcessor = new TestPostProcessor();
        source.RegisterPostProcessor(postProcessor);

        var provider = new KubernetesServiceBindingConfigurationProvider(source);
        provider.Load();

        postProcessor.PostProcessorCalled.Should().BeTrue();
    }

    private static string GetK8SResourcesDirectory(string name)
    {
        return Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "resources", "k8s", name);
    }

    private static string GetEmptyK8SResourcesDirectory()
    {
        return Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "resources", "k8s-empty");
    }

    private sealed class TestPostProcessor : IConfigurationPostProcessor
    {
        public bool PostProcessorCalled { get; set; }

        public void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string?> configurationData)
        {
            PostProcessorCalled = true;
        }
    }
}
