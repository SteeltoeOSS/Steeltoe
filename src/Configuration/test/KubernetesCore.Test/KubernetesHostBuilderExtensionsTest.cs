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
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common;
using System;
using System.Linq;
using Xunit;

namespace Steeltoe.Extensions.Configuration.Kubernetes.Test
{
    public class KubernetesHostBuilderExtensionsTest
    {
        [Fact]
        public void AddKubernetesConfiguration_DefaultWebHost_AddsConfig()
        {
            // Arrange
            var hostBuilder = WebHost.CreateDefaultBuilder().UseStartup<TestServerStartup>();

            // Act
            hostBuilder.AddKubernetesConfiguration(FakeClientSetup);
            var serviceProvider = hostBuilder.Build().Services;
            var config = serviceProvider.GetServices<IConfiguration>().SingleOrDefault() as ConfigurationRoot;
            var appInfo = serviceProvider.GetServices<IApplicationInstanceInfo>().SingleOrDefault();

            // Assert
            Assert.Contains(config.Providers, ics => ics.GetType().IsAssignableFrom(typeof(KubernetesConfigMapProvider)));
            Assert.Contains(config.Providers, ics => ics.GetType().IsAssignableFrom(typeof(KubernetesSecretProvider)));
            Assert.IsAssignableFrom<KubernetesApplicationOptions>(appInfo);
        }

        [Fact]
        public void AddKubernetesConfiguration_WebHostBuilder_AddsConfig()
        {
            // Arrange
            var hostBuilder = new WebHostBuilder().UseStartup<TestServerStartup>();

            // Act
            hostBuilder.AddKubernetesConfiguration(FakeClientSetup);
            var serviceProvider = hostBuilder.Build().Services;
            var config = serviceProvider.GetServices<IConfiguration>().SingleOrDefault() as ConfigurationRoot;
            var appInfo = serviceProvider.GetServices<IApplicationInstanceInfo>().SingleOrDefault();

            // Assert
            Assert.Contains(config.Providers, ics => ics.GetType().IsAssignableFrom(typeof(KubernetesConfigMapProvider)));
            Assert.Contains(config.Providers, ics => ics.GetType().IsAssignableFrom(typeof(KubernetesSecretProvider)));
            Assert.IsAssignableFrom<KubernetesApplicationOptions>(appInfo);
        }

        [Fact]
        public void AddKubernetesConfiguration_DefaultHost_AddsConfig()
        {
            // Arrange
            var hostBuilder = Host.CreateDefaultBuilder().ConfigureWebHostDefaults(builder => builder.UseStartup<TestServerStartup>());

            // Act
            hostBuilder.AddKubernetesConfiguration(FakeClientSetup);
            var serviceProvider = hostBuilder.Build().Services;
            var config = serviceProvider.GetServices<IConfiguration>().SingleOrDefault() as ConfigurationRoot;
            var appInfo = serviceProvider.GetServices<IApplicationInstanceInfo>().SingleOrDefault();

            // Assert
            Assert.Contains(config.Providers, ics => ics.GetType().IsAssignableFrom(typeof(KubernetesConfigMapProvider)));
            Assert.Contains(config.Providers, ics => ics.GetType().IsAssignableFrom(typeof(KubernetesSecretProvider)));
            Assert.IsAssignableFrom<KubernetesApplicationOptions>(appInfo);
        }

        [Fact]
        public void AddKubernetesConfiguration_HostBuilder_AddsConfig()
        {
            // Arrange
            var hostBuilder = new HostBuilder().AddKubernetesConfiguration(FakeClientSetup);

            // Act
            var serviceProvider = hostBuilder.Build().Services;
            var config = serviceProvider.GetServices<IConfiguration>().SingleOrDefault() as ConfigurationRoot;
            var appInfo = serviceProvider.GetServices<IApplicationInstanceInfo>().SingleOrDefault();

            // Assert
            Assert.Contains(config.Providers, ics => ics.GetType().IsAssignableFrom(typeof(KubernetesConfigMapProvider)));
            Assert.Contains(config.Providers, ics => ics.GetType().IsAssignableFrom(typeof(KubernetesSecretProvider)));
            Assert.IsAssignableFrom<KubernetesApplicationOptions>(appInfo);
        }

        // TODO: need some kind of action here for tests to pass if no kube config is found
        private Action<KubernetesClientConfiguration> FakeClientSetup => (fakeClient) => fakeClient.Namespace = "default"; // fakeClient.Host = "http://127.0.0.1";
    }
}
