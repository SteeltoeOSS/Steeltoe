// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Extensions.Configuration.Kubernetes.Test
{
    public class KubernetesConfigurationBuilderExtensionsTest
    {
        [Fact]
        public void AddKubernetes_ThrowsIfConfigBuilderNull()
        {
            // Arrange
            IConfigurationBuilder configurationBuilder = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => KubernetesConfigurationBuilderExtensions.AddKubernetes(configurationBuilder));
            Assert.Contains(nameof(configurationBuilder), ex.Message);
        }

        [Fact]
        public void AddKubernetes_Enabled_AddsConfigMapAndSecretsToSourcesList()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder();

            // Act and Assert
            configurationBuilder.AddKubernetes(FakeClientSetup);

            Assert.Contains(configurationBuilder.Sources, ics => ics.GetType().IsAssignableFrom(typeof(KubernetesConfigMapSource)));
            Assert.Contains(configurationBuilder.Sources, ics => ics.GetType().IsAssignableFrom(typeof(KubernetesSecretSource)));
        }

        [Fact]
        public void AddKubernetes_Disabled_DoesntAddConfigMapAndSecretsToSourcesList()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> { { "spring:cloud:kubernetes:enabled", "false" } });

            // Act and Assert
            configurationBuilder.AddKubernetes(FakeClientSetup);

            Assert.DoesNotContain(configurationBuilder.Sources, ics => ics.GetType().IsAssignableFrom(typeof(KubernetesConfigMapSource)));
            Assert.DoesNotContain(configurationBuilder.Sources, ics => ics.GetType().IsAssignableFrom(typeof(KubernetesSecretSource)));
        }

        [Fact]
        public void AddKubernetes_ConfigMapDisabled_DoesntAddConfigMapToSourcesList()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> { { "spring:cloud:kubernetes:config:enabled", "false" } });

            // Act and Assert
            configurationBuilder.AddKubernetes(FakeClientSetup);

            Assert.DoesNotContain(configurationBuilder.Sources, ics => ics.GetType().IsAssignableFrom(typeof(KubernetesConfigMapSource)));
            Assert.Contains(configurationBuilder.Sources, ics => ics.GetType().IsAssignableFrom(typeof(KubernetesSecretSource)));
        }

        [Fact]
        public void AddKubernetes_SecretsDisabled_DoesntAddSecretsToSourcesList()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> { { "spring:cloud:kubernetes:secrets:enabled", "false" } });

            // Act and Assert
            configurationBuilder.AddKubernetes(FakeClientSetup);

            Assert.Contains(configurationBuilder.Sources, ics => ics.GetType().IsAssignableFrom(typeof(KubernetesConfigMapSource)));
            Assert.DoesNotContain(configurationBuilder.Sources, ics => ics.GetType().IsAssignableFrom(typeof(KubernetesSecretSource)));
        }

        private Action<KubernetesClientConfiguration> FakeClientSetup => (fakeClient) => fakeClient.Host = "http://127.0.0.1";
    }
}
