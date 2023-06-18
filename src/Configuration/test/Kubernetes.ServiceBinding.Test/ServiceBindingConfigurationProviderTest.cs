// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Steeltoe.Configuration.Kubernetes.ServiceBinding.Test;

public sealed class ServiceBindingConfigurationProviderTest
{
    [Fact]
    public void EnvironmentVariableNotSet()
    {
        // Optional defaults true, no throw
        var source = new ServiceBindingConfigurationSource();
        var provider = new ServiceBindingConfigurationProvider(source);

        provider.Load();

        // Optional, no throw
        source = new ServiceBindingConfigurationSource
        {
            Optional = false
        };

        // Not optional, should throw
        provider = new ServiceBindingConfigurationProvider(source);
        Action action = () => provider.Load();
        action.Should().ThrowExactly<DirectoryNotFoundException>();
    }

    [Fact]
    public void EnvironmentVariableSet_InvalidDirectory()
    {
        string rootDir = GetK8SResourcesDirectory("invalid");
        Environment.SetEnvironmentVariable(ServiceBindingConfigurationSource.ServiceBindingRootDirEnvVariable, rootDir);

        try
        {
            // Not optional, should throw
            var source = new ServiceBindingConfigurationSource();
            var provider = new ServiceBindingConfigurationProvider(source);
            provider.Load();

            // Optional, no throw
            source = new ServiceBindingConfigurationSource
            {
                Optional = false
            };

            provider = new ServiceBindingConfigurationProvider(source);
            Action action = () => provider.Load();
            action.Should().ThrowExactly<DirectoryNotFoundException>();
        }
        finally
        {
            Environment.SetEnvironmentVariable(ServiceBindingConfigurationSource.ServiceBindingRootDirEnvVariable, null);
        }
    }

    [Fact]
    public void EnvironmentVariableSet_ValidDirectory()
    {
        string rootDir = GetK8SResourcesDirectory(null);
        Environment.SetEnvironmentVariable(ServiceBindingConfigurationSource.ServiceBindingRootDirEnvVariable, rootDir);

        try
        {
            var source = new ServiceBindingConfigurationSource();
            var provider = new ServiceBindingConfigurationProvider(source);
            provider.Load();

            provider.TryGet("k8s:bindings:test-name-1:type", out string value).Should().BeTrue();
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
        finally
        {
            Environment.SetEnvironmentVariable(ServiceBindingConfigurationSource.ServiceBindingRootDirEnvVariable, null);
        }
    }

    [Fact]
    public void NoBindings_DoesNotThrow()
    {
        var builder = new ConfigurationBuilder();
        builder.Add(new ServiceBindingConfigurationSource(GetEmptyK8SResourcesDirectory()));

        Action action = () => builder.Build();
        action.Should().NotThrow();
    }

    [Fact]
    public void PostProcessors_OnByDefault()
    {
        string rootDir = GetK8SResourcesDirectory(null);

        var source = new ServiceBindingConfigurationSource(rootDir);
        var postProcessor = new TestPostProcessor();
        source.RegisterPostProcessor(postProcessor);

        var provider = new ServiceBindingConfigurationProvider(source);
        provider.Load();

        postProcessor.PostProcessorCalled.Should().BeTrue();
    }

    [Fact]
    public void PostProcessors_CanBeDisabled()
    {
        string rootDir = GetK8SResourcesDirectory(null);

        var source = new ServiceBindingConfigurationSource(rootDir)
        {
            ParentConfiguration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
            {
                { "steeltoe:kubernetes:service-bindings:enable", "false" }
            }).Build()
        };

        var postProcessor = new TestPostProcessor();
        source.RegisterPostProcessor(postProcessor);

        var provider = new ServiceBindingConfigurationProvider(source);
        provider.Load();

        postProcessor.PostProcessorCalled.Should().BeFalse();
    }

    private static string GetK8SResourcesDirectory(string name)
    {
        return Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "resources", "k8s", $"{name}");
    }

    private static string GetEmptyK8SResourcesDirectory()
    {
        return Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "resources", "k8s-empty");
    }

    private sealed class TestPostProcessor : IConfigurationPostProcessor
    {
        public bool PostProcessorCalled { get; set; }

        public void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string> configurationData)
        {
            PostProcessorCalled = true;
        }
    }
}
