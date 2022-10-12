// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public void EnvironmentVariableSet()
    {
        var rootDir = Path.Combine(Environment.CurrentDirectory, "resources\\k8s");
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

    [Fact]
    public void PostProcessorRuns()
    {
        var rootDir = Path.Combine(Environment.CurrentDirectory, "resources\\k8s");
        Environment.SetEnvironmentVariable(ServiceBindingConfigurationSource.ServiceBindingRootDirEnvVariable, rootDir);
        try
        {
            var source = new ServiceBindingConfigurationSource();
            source.RegisterPostProcessor(new ConfigServerPostProcessor());

            var provider = new ServiceBindingConfigurationProvider(source);
            provider.Load();

            Assert.True(provider.TryGet("k8s:bindings:test-config-server-1:type", out string value));
            Assert.Equal("config", value);
            Assert.True(provider.TryGet("k8s:bindings:test-config-server-1:provider", out value));
            Assert.Equal("test-provider-1", value);

            Assert.True(provider.TryGet("k8s:bindings:test-config-server-1:uri", out value));
            Assert.Equal("uri-value", value);
            Assert.True(provider.TryGet("k8s:bindings:test-config-server-1:access-token-uri", out value));
            Assert.Equal("access-token-uri-value", value);
            Assert.True(provider.TryGet("k8s:bindings:test-config-server-1:client-id", out value));
            Assert.Equal("client-id-value", value);
            Assert.True(provider.TryGet("k8s:bindings:test-config-server-1:client-secret", out value));
            Assert.Equal("client-secret-value", value);

            // Check for post processor output
            Assert.True(provider.TryGet("spring:cloud:config:uri", out value));
            Assert.Equal("uri-value", value);
            Assert.True(provider.TryGet("spring:cloud:config:client:oauth2:clientId", out value));
            Assert.Equal("client-id-value", value);
            Assert.True(provider.TryGet("spring:cloud:config:client:oauth2:clientSecret", out value));
            Assert.Equal("client-secret-value", value);
            Assert.True(provider.TryGet("spring:cloud:config:client:oauth2:accessTokenUri", out value));
            Assert.Equal("access-token-uri-value", value);

        }
        finally
        {
            Environment.SetEnvironmentVariable(ServiceBindingConfigurationSource.ServiceBindingRootDirEnvVariable, null);
        }
    }
}
