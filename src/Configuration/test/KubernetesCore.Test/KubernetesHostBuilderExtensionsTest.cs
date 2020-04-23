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

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Steeltoe.Extensions.Configuration.Kubernetes;
using System.Linq;
using Xunit;

namespace Steeltoe.Extensions.Configuration.Kubernetes.Test
{
    public class KubernetesHostBuilderExtensionsTest
    {
        [Fact]
        public void AddKubernetesConfig_DefaultWebHost_AddsConfigMaps()
        {
            // Arrange
            var hostBuilder = WebHost.CreateDefaultBuilder().UseStartup<TestServerStartup>();

            // Act
            hostBuilder.AddKubernetesConfiguration();
            var config = hostBuilder.Build().Services.GetServices<IConfiguration>().SingleOrDefault() as ConfigurationRoot;

            // Assert
            Assert.Contains(It.IsAny<KubernetesConfigMapProvider>(), config.Providers);
            Assert.Contains(It.IsAny<KubernetesSecretProvider>(), config.Providers);
        }

        [Fact]
        public void AddKubernetes_New_WebHostBuilder_AddsKubernetes()
        {
            // Arrange
            var hostBuilder = new WebHostBuilder().UseStartup<TestServerStartup>();

            // Act
            hostBuilder.AddKubernetesConfiguration();
            var config = hostBuilder.Build().Services.GetServices<IConfiguration>().SingleOrDefault() as ConfigurationRoot;

            // Assert
            Assert.Contains(It.IsAny<KubernetesConfigMapProvider>(), config.Providers);
            Assert.Contains(It.IsAny<KubernetesSecretProvider>(), config.Providers);
        }

        [Fact]
        public void AddKubernetes_IHostBuilder_AddsKubernetes()
        {
            // Arrange
            var hostBuilder = new HostBuilder().AddKubernetesConfiguration();

            // Act
            var host = hostBuilder.Build();
            var config = host.Services.GetServices<IConfiguration>().SingleOrDefault() as ConfigurationRoot;

            // Assert
            Assert.Contains(It.IsAny<KubernetesConfigMapProvider>(), config.Providers);
            Assert.Contains(It.IsAny<KubernetesSecretProvider>(), config.Providers);
        }
    }
}
