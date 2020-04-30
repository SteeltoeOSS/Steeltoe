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
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Steeltoe.Common.Kubernetes.Test
{
    public class IServiceCollectionExtensionsTest
    {
        [Fact]
        public void AddKubernetesApplicationInstanceInfo_ThrowsOnNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => IServiceCollectionExtensions.AddKubernetesApplicationInstanceInfo(null));
            Assert.Equal("serviceCollection", ex.ParamName);
        }

        [Fact]
        public void AddKubernetesApplicationInstanceInfo_ReplacesExistingAppInfo()
        {
            // arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            Assert.NotNull(serviceCollection.GetApplicationInstanceInfo());

            // act
            serviceCollection.AddKubernetesApplicationInstanceInfo();
            var serviceProvider = serviceCollection.BuildServiceProvider();

            // assert
            var appInfos = serviceProvider.GetServices<IApplicationInstanceInfo>();
            Assert.Single(appInfos);
            Assert.NotNull(appInfos.FirstOrDefault());
            Assert.IsType<KubernetesApplicationOptions>(appInfos.FirstOrDefault());
        }

        [Fact]
        public void GetKubernetesApplicationOptions_ThrowsOnNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => IServiceCollectionExtensions.GetKubernetesApplicationOptions(null));
            Assert.Equal("serviceCollection", ex.ParamName);
        }

        [Fact]
        public void GetKubernetesApplicationOptions_ReturnsAndAddsOptions()
        {
            // arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

            // act
            var options = serviceCollection.GetKubernetesApplicationOptions();
            var appInfos = serviceCollection.BuildServiceProvider().GetServices<IApplicationInstanceInfo>();

            // assert
            Assert.NotNull(options);
            Assert.Single(appInfos);
            Assert.Equal(options, appInfos.FirstOrDefault());
            Assert.IsType<KubernetesApplicationOptions>(options);
            Assert.Equal(Assembly.GetEntryAssembly().GetName().Name, options.ApplicationName);
        }

        [Fact]
        public void AddKubernetesClient_ThrowsOnNulls()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => IServiceCollectionExtensions.AddKubernetesClient(null));
            Assert.Equal("serviceCollection", ex.ParamName);
        }

        [Fact]
        public void AddKubernetesClient_AddsKubernetesOptionsAndClient()
        {
            // arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

            // act
            serviceCollection.AddKubernetesClient();
            var client = serviceCollection.BuildServiceProvider().GetService<IKubernetes>();
            var appInfos = serviceCollection.BuildServiceProvider().GetServices<IApplicationInstanceInfo>();

            // assert
            Assert.NotNull(client);
            Assert.Single(appInfos);
            Assert.IsType<KubernetesApplicationOptions>(appInfos.First());
            Assert.Equal(Assembly.GetEntryAssembly().GetName().Name, appInfos.First().ApplicationName);
        }
    }
}
