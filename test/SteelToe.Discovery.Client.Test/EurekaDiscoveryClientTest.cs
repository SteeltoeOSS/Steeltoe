//
// Copyright 2015 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using System;
using Xunit;
using SteelToe.Discovery.Eureka;

namespace SteelToe.Discovery.Client.Test
{
    public class EurekaDiscoveryClientTest: AbstractBaseTest
    { 
     

        [Fact]
        public void Constructor_ThrowsIfClientOptionsNull()
        {
            // Arrange
            EurekaClientOptions clientOptions = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new EurekaDiscoveryClient(clientOptions, null));
            Assert.Contains(nameof(clientOptions), ex.Message);
        }

        [Fact]
        public void Constructor_Initializes_Correctly()
        {
            var clientConfig = new EurekaClientOptions()
            {
                ShouldRegisterWithEureka = false,
                ShouldFetchRegistry = false
            };

            var instConfig = new EurekaInstanceOptions();

            var client = new EurekaDiscoveryClient(clientConfig, instConfig);

            Assert.NotNull(client.Client);
            Assert.NotNull(client.ClientConfig);
            Assert.NotNull(client.InstConfig);

            Assert.NotNull(ApplicationInfoManager.Instance.InstanceConfig);
            Assert.NotNull(ApplicationInfoManager.Instance.InstanceInfo);

            Assert.NotNull(DiscoveryManager.Instance.ClientConfig);
            Assert.NotNull(DiscoveryManager.Instance.Client);
            Assert.NotNull(DiscoveryManager.Instance.InstanceConfig);

        }

        [Fact]
        public void ConfigureInstanceIfNeeded_Intializes_InstanceConfig()
        {
            var clientConfig = new EurekaClientOptions()
            {
                ShouldRegisterWithEureka = false,
                ShouldFetchRegistry = false
            };
            var instConfig = new EurekaInstanceOptions();

            var client = new EurekaDiscoveryClient(clientConfig, instConfig);

            Assert.NotNull(client.Client);
            Assert.NotNull(client.ClientConfig);
            Assert.NotNull(client.InstConfig);

            Assert.Equal("unknown", client.InstConfig.AppName);
            Assert.Equal("unknown", client.InstConfig.VirtualHostName);
            var expectId = client.InstConfig.GetHostName(false) + ":" + "unknown" + ":" + "8080";
            Assert.Equal(expectId, client.InstConfig.InstanceId);
        }
    }
}
