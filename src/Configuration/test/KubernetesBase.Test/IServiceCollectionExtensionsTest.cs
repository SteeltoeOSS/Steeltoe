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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common;
using System;
using System.Linq;
using Xunit;

namespace Steeltoe.Extensions.Configuration.Kubernetes.Test
{
    public class IServiceCollectionExtensionsTest
    {
        [Fact]
        public void RegisterKubernetesApplicationInstanceInfo_ThrowsOnNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => IServiceCollectionExtensions.RegisterKubernetesApplicationInstanceInfo(null));
            Assert.Equal("serviceCollection", ex.ParamName);
        }

        [Fact]
        public void RegisterKubernetesApplicationInstanceInfo_ReplacesExistingAppInfo()
        {
            // arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            Assert.NotNull(serviceCollection.GetApplicationInstanceInfo());

            // act
            serviceCollection.RegisterKubernetesApplicationInstanceInfo();
            var serviceProvider = serviceCollection.BuildServiceProvider();

            // assert
            var appInfos = serviceProvider.GetServices<IApplicationInstanceInfo>();
            Assert.Single(appInfos);
            Assert.NotNull(appInfos.FirstOrDefault());
            Assert.IsType<KubernetesApplicationOptions>(appInfos.FirstOrDefault());
        }
    }
}
