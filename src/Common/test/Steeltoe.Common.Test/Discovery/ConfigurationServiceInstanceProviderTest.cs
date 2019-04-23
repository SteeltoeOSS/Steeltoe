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

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Steeltoe.Common.Discovery.Test
{
    public class ConfigurationServiceInstanceProviderTest
    {
        [Fact]
        public void Returns_ConfiguredServices()
        {
            // arrange
            var services = new List<ConfigurationServiceInstance>
            {
                new ConfigurationServiceInstance { ServiceId = "fruitService", Host = "fruitball", Port = 443, IsSecure = true },
                new ConfigurationServiceInstance { ServiceId = "fruitService", Host = "fruitballer", Port = 8081 },
                new ConfigurationServiceInstance { ServiceId = "fruitService", Host = "fruitballerz", Port = 8082 },
                new ConfigurationServiceInstance { ServiceId = "vegetableService", Host = "vegemite", Port = 443, IsSecure = true },
                new ConfigurationServiceInstance { ServiceId = "vegetableService", Host = "carrot", Port = 8081 },
                new ConfigurationServiceInstance { ServiceId = "vegetableService", Host = "beet", Port = 8082 },
            };
            var serviceOptions = new TestOptionsMonitor<List<ConfigurationServiceInstance>>(services);

            // act
            var provider = new ConfigurationServiceInstanceProvider(serviceOptions);

            // assert
            Assert.Equal(3, provider.GetInstances("fruitService").Count);
            Assert.Equal(3, provider.GetInstances("vegetableService").Count);
            Assert.Equal(2, provider.Services.Count);
        }

        [Fact]
        public void ReceivesUpdatesTo_ConfiguredServices()
        {
            // arrange
            var services = new List<ConfigurationServiceInstance>
            {
                new ConfigurationServiceInstance { ServiceId = "fruitService", Host = "fruitball", Port = 443, IsSecure = true },
            };
            var serviceOptions = new TestOptionsMonitor<List<ConfigurationServiceInstance>>(services);
            var provider = new ConfigurationServiceInstanceProvider(serviceOptions);
            Assert.Single(provider.GetInstances("fruitService"));
            Assert.Equal("fruitball", provider.GetInstances("fruitService").First().Host);

            // act
            services.First().Host = "updatedValue";

            // assert
            Assert.Equal("updatedValue", provider.GetInstances("fruitService").First().Host);
        }
    }
}
