﻿// Copyright 2017 the original author or authors.
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

using Microsoft.Extensions.Configuration;
using Moq;
using System;
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
            var ex = Assert.Throws<ArgumentNullException>(() => KubernetesConfigurationBuilderExtensions.AddKubernetesConfiguration(configurationBuilder));
            Assert.Contains(nameof(configurationBuilder), ex.Message);
        }

        [Fact]
        public void AddKubernetes_AddsConfigMapAndSecretsToSourcesList()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder();

            // Act and Assert
            configurationBuilder.AddKubernetesConfiguration();

            Assert.Contains(It.IsAny<KubernetesConfigMapSource>(), configurationBuilder.Sources);
            Assert.Contains(It.IsAny<KubernetesSecretSource>(), configurationBuilder.Sources);
        }
    }
}
