// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Steeltoe.Configuration.Kubernetes.ServiceBinding.Test;
public class ServiceBindingConfigurationProviderTest
{
    [Fact]
    public void EnvironmentVariableNotSet()
    {
        // Not optional, should throw
        var source = new ServiceBindingConfigurationSource();
               var provider = new ServiceBindingConfigurationProvider(source);
        Assert.Throws<DirectoryNotFoundException>(() => provider.Load());

        // Optional, no throw
        source = new ServiceBindingConfigurationSource()
        {
            Optional = true
        };
        provider = new ServiceBindingConfigurationProvider(source);
        provider.Load();
    }

    [Fact]
    public void EnvironmentVariableSet_InvalidDirectory()
    {
        var rootDir = GetK8SResourcesDirectory("invalid");
        Environment.SetEnvironmentVariable(ServiceBindingConfigurationSource.ServiceBindingRootDirEnvVariable, rootDir);
        try
        {
            // Not optional, should throw
            var source = new ServiceBindingConfigurationSource();
            var provider = new ServiceBindingConfigurationProvider(source);
            Assert.Throws<DirectoryNotFoundException>(() => provider.Load());

            // Optional, no throw
            source = new ServiceBindingConfigurationSource()
            {
                Optional = true
            };
            provider = new ServiceBindingConfigurationProvider(source);
            provider.Load();

        }
        finally
        {
            Environment.SetEnvironmentVariable(ServiceBindingConfigurationSource.ServiceBindingRootDirEnvVariable, null);
        }
    }

    [Fact]
    public void EnvironmentVariableSet_ValidDirectory()
    {
        var rootDir = GetK8SResourcesDirectory(null);
        Environment.SetEnvironmentVariable(ServiceBindingConfigurationSource.ServiceBindingRootDirEnvVariable, rootDir);
        try
        {
            var source = new ServiceBindingConfigurationSource();
            var provider = new ServiceBindingConfigurationProvider(source);
            provider.Load();

            Assert.True(provider.TryGet("k8s:bindings:test-name-1:type", out string value));
            Assert.Equal("test-type-1", value);
            Assert.True(provider.TryGet("k8s:bindings:test-name-1:provider", out value));
            Assert.Equal("test-provider-1", value);
            Assert.True(provider.TryGet("k8s:bindings:test-name-1:test-secret-key", out value));
            Assert.Equal("test-secret-value", value);

            Assert.True(provider.TryGet("k8s:bindings:test-name-2:type", out value));
            Assert.Equal("test-type-2", value);
            Assert.True(provider.TryGet("k8s:bindings:test-name-2:provider", out value));
            Assert.Equal("test-provider-2", value);
            Assert.True(provider.TryGet("k8s:bindings:test-name-2:test-secret-key", out value));
            Assert.Equal("test-secret-value", value);

            Assert.True(provider.TryGet("k8s:bindings:test-k8s:type", out value));
            Assert.Equal("test-type-1", value);
            Assert.True(provider.TryGet("k8s:bindings:test-k8s:provider", out value));
            Assert.Equal("test-provider-1", value);
            Assert.True(provider.TryGet("k8s:bindings:test-k8s:test-secret-key", out value));
            Assert.Equal("test-secret-value", value);

        }
        finally
        {
            Environment.SetEnvironmentVariable(ServiceBindingConfigurationSource.ServiceBindingRootDirEnvVariable, null);
        }
    }

    [Fact(Skip = "TODO: fix this failing test")]
    public void NoBindings()
    {
        var builder = new ConfigurationBuilder();
        builder.Add(new ServiceBindingConfigurationSource(GetEmptyK8SResourcesDirectory()));    
        var configuration = builder.Build();
        Assert.NotNull(configuration);
        Assert.Throws<InvalidOperationException>(() => configuration.GetRequiredSection("k8s"));
    }


    [Fact]
    public void PostProcessors_DisabledbyDefault()
    {
        var rootDir = GetK8SResourcesDirectory(null);

        var source = new ServiceBindingConfigurationSource(rootDir);
        var postProcessor = new TestPostProcessor();
        source.RegisterPostProcessor(postProcessor);

        var provider = new ServiceBindingConfigurationProvider(source);
        provider.Load();

        Assert.False(postProcessor.PostProcessorCalled);
    }

    [Fact]
    public void PostProcessors_CanBeEnabled()
    {
        var rootDir = GetK8SResourcesDirectory(null);

        var source = new ServiceBindingConfigurationSource(rootDir);
        source.ParentConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>() { { "steeltoe:kubernetes:bindings:enable", "true" } })
            .Build();
        var postProcessor = new TestPostProcessor();
        source.RegisterPostProcessor(postProcessor);

        var provider = new ServiceBindingConfigurationProvider(source);
        provider.Load();

        Assert.True(postProcessor.PostProcessorCalled);
    }


    private static string GetK8SResourcesDirectory(string name)
    {
        return Path.Combine(Environment.CurrentDirectory, $"..\\..\\..\\resources\\k8s\\{name}");
    }

    private static string GetEmptyK8SResourcesDirectory()
    {
        return Path.Combine(Environment.CurrentDirectory, $"..\\..\\..\\resources\\k8s-empty\\");
    }

    private class TestPostProcessor : IConfigurationPostProcessor
    {
        public bool PostProcessorCalled { get; set; }

        public void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string> configData)
        {
            PostProcessorCalled = true;
        }
    }
}
