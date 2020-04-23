// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
