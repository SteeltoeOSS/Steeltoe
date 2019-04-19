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

using System;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test
{
    public class DiscoveryManagerTest : AbstractBaseTest
    {
        [Fact]
        public void DiscoveryManager_IsSingleton()
        {
            Assert.Equal(ApplicationInfoManager.Instance, ApplicationInfoManager.Instance);
        }

        [Fact]
        public void DiscoveryManager_Uninitialized()
        {
            Assert.Null(DiscoveryManager.Instance.Client);
            Assert.Null(DiscoveryManager.Instance.ClientConfig);
            Assert.Null(DiscoveryManager.Instance.InstanceConfig);
        }

        [Fact]
        public void Initialize_Throws_IfInstanceConfigNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => DiscoveryManager.Instance.Initialize(new EurekaClientConfig(), (EurekaInstanceConfig)null, null));
            Assert.Contains("instanceConfig", ex.Message);
        }

        [Fact]
        public void Initialize_Throws_IfClientConfigNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => DiscoveryManager.Instance.Initialize(null, new EurekaInstanceConfig(), null));
            Assert.Contains("clientConfig", ex.Message);
        }

        [Fact]
        public void Initialize_WithBothConfigs_InitializesAll()
        {
            EurekaInstanceConfig instanceConfig = new EurekaInstanceConfig();
            EurekaClientConfig clientConfig = new EurekaClientConfig()
            {
                ShouldRegisterWithEureka = false,
                ShouldFetchRegistry = false
            };
            DiscoveryManager.Instance.Initialize(clientConfig, instanceConfig);

            Assert.NotNull(DiscoveryManager.Instance.InstanceConfig);
            Assert.Equal(instanceConfig, DiscoveryManager.Instance.InstanceConfig);
            Assert.NotNull(DiscoveryManager.Instance.ClientConfig);
            Assert.Equal(clientConfig, DiscoveryManager.Instance.ClientConfig);
            Assert.NotNull(DiscoveryManager.Instance.Client);

            Assert.Equal(instanceConfig, ApplicationInfoManager.Instance.InstanceConfig);
        }

        [Fact]
        public void Initialize_WithClientConfig_InitializesAll()
        {
            EurekaClientConfig clientConfig = new EurekaClientConfig()
            {
                ShouldRegisterWithEureka = false,
                ShouldFetchRegistry = false
            };
            DiscoveryManager.Instance.Initialize(clientConfig);

            Assert.Null(DiscoveryManager.Instance.InstanceConfig);
            Assert.NotNull(DiscoveryManager.Instance.ClientConfig);
            Assert.Equal(clientConfig, DiscoveryManager.Instance.ClientConfig);
            Assert.NotNull(DiscoveryManager.Instance.Client);

            Assert.Null(ApplicationInfoManager.Instance.InstanceConfig);
        }
    }
}
