// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Steeltoe.Configuration.Kubernetes.Test;

public sealed class KubernetesConfigurationBuilderExtensionsTest
{
    private static Action<KubernetesClientConfiguration> FakeClientSetup => fakeClient => fakeClient.Host = "http://127.0.0.1";

    [Fact]
    public void AddKubernetes_Enabled_AddsConfigMapAndSecretsToSourcesList()
    {
        var configurationBuilder = new ConfigurationBuilder();

        configurationBuilder.AddKubernetes(FakeClientSetup);

        Assert.Contains(configurationBuilder.Sources, source => source.GetType().IsAssignableFrom(typeof(KubernetesConfigMapSource)));
        Assert.Contains(configurationBuilder.Sources, source => source.GetType().IsAssignableFrom(typeof(KubernetesSecretSource)));
    }

    [Fact]
    public void AddKubernetes_Disabled_DoesNotAddConfigMapAndSecretsToSourcesList()
    {
        IConfigurationBuilder configurationBuilder = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "spring:cloud:kubernetes:enabled", "false" }
        });

        configurationBuilder.AddKubernetes(FakeClientSetup);

        Assert.DoesNotContain(configurationBuilder.Sources, source => source.GetType().IsAssignableFrom(typeof(KubernetesConfigMapSource)));
        Assert.DoesNotContain(configurationBuilder.Sources, source => source.GetType().IsAssignableFrom(typeof(KubernetesSecretSource)));
    }

    [Fact]
    public void AddKubernetes_ConfigMapDisabled_DoesNotAddConfigMapToSourcesList()
    {
        IConfigurationBuilder configurationBuilder = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "spring:cloud:kubernetes:config:enabled", "false" }
        });

        configurationBuilder.AddKubernetes(FakeClientSetup);

        Assert.DoesNotContain(configurationBuilder.Sources, source => source.GetType().IsAssignableFrom(typeof(KubernetesConfigMapSource)));
        Assert.Contains(configurationBuilder.Sources, source => source.GetType().IsAssignableFrom(typeof(KubernetesSecretSource)));
    }

    [Fact]
    public void AddKubernetes_SecretsDisabled_DoesNotAddSecretsToSourcesList()
    {
        IConfigurationBuilder configurationBuilder = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "spring:cloud:kubernetes:secrets:enabled", "false" }
        });

        configurationBuilder.AddKubernetes(FakeClientSetup);

        Assert.Contains(configurationBuilder.Sources, source => source.GetType().IsAssignableFrom(typeof(KubernetesConfigMapSource)));
        Assert.DoesNotContain(configurationBuilder.Sources, source => source.GetType().IsAssignableFrom(typeof(KubernetesSecretSource)));
    }
}
